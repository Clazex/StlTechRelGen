using System.Diagnostics.CodeAnalysis;

using Spectre.Console.Cli;

using StlTechRelGen.Db;

namespace StlTechRelGen;

public static class Program {
	public static async Task Main(string[] args) {
		try {
			Initialize();
			Config config = ResolveConfig(args);
			await Execute(config);
		} catch (Exception e) {
			AnsiConsole.WriteException(e);
		}
	}

	private static void Initialize() {
		// Add support for codepage 1252, used by CWTools
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

		// Initialize SQLitePCL using the packages:
		// SQLitePCLRaw.config.e_sqlite3 (glue) + SourceGear.sqlite3 (binary).
		SQLitePCL.Batteries_V2.Init();
	}

	private static Config ResolveConfig(string[] args) {
		if (args.Length == 0) {
			Config config = Config.Load() ?? Inquiries.RunSetupWizard();
			config.Save(); // Roundtrip to format, or save newly created
			return config;
		}

		CommandApp<CliCommand> app = new();
		app.Configure(config =>
			config.UseAssemblyInformationalVersion()
		);
		app.Run(args);

		if (CliCommand.Arguments is not CliCommand.Settings arguments) {
			// Main command was not invoked, e.g. -h or -v was used
			Environment.Exit(0);
			return null; // Unreachable
		} else {
			return arguments.ToConfig();
		}
	}

	private static async Task Execute(Config config) {
		UpdateChecker? updateChecker = config.Update.CheckUpdate
			? new UpdateChecker(config.Update) : null;

		if (config.OverrideLanguage != null) {
			LoadMessages(config.OverrideLanguage);
		}

		if (config.SuppressesCWToolsErrors) {
			// Normally there will be two lines of error complaining missing rule files
			CWTools.Utilities.Utils.logError = FuncConvert.FromAction((string _) => { });
		}

		Inquiries.WaitForLauncherClose(config);

		(string destPath, Mod[] mods) = await GetTarget(config);
		DirectoryInfo l11nDir = new(Path.Combine(destPath, "localisation"));
		PrepareOutputDir(l11nDir);

		(int countL11nFiles, int countTechs, int countRelations) = await AnsiConsole
			.Progress()
			.UsePreset()
			.StartAsync(async (ctx) => {
				GameData gameData = LoadGameData(ctx, config, mods);
				L11nBuilder l11nBuilder = BuildL11n(ctx, gameData);
				WriteL11nFragments(ctx, l11nDir);
				l11nBuilder.WriteFilesWithProgress(ctx, l11nDir.CreateSubdirectory("replace").FullName);

				return (
					l11nBuilder.Generated.Keys.Count,
					gameData.TechTable.Techs.Count,
					// On Requires side we'll need to deal with alternatives
					gameData.TechTable.Techs.Values.Sum(i => i.Unlocks.Count) * 2
				);
			});

		AnsiConsole.MarkupLine(Messages.Prompt.SavedLocalization.Format(countL11nFiles, l11nDir.FullName));
		AnsiConsole.MarkupLine(Messages.Prompt.GenerationSummary.Format(countRelations, countTechs));
		if (!config.Yesmen) {
			Inquiries.Pause();
		}

		updateChecker?.TryReport();
	}


	private static void PrepareOutputDir(DirectoryInfo outputDirectory) {
		try {
			if (outputDirectory.Exists) {
				outputDirectory.Delete(true);
			}

			outputDirectory.Create();
		} catch {
			LogError(Messages.Error.FailedToAccessOutputDir.Format(outputDirectory.FullName));
			throw;
		}
	}

	private static GameData LoadGameData(ProgressContext ctx, Config config, Mod[] sourceMods) =>
		GameData.LoadWithProgress(ctx, config, sourceMods.Select(i => (i.DisplayName!, i.Path())));

	private static L11nBuilder BuildL11n(ProgressContext ctx, GameData gameData) {
		L11nBuilder l11nBuilder = new(gameData);
		gameData.TechTable.Techs
			.DriveProgressTask(ctx.AddTask(Messages.Progress.GeneratingLocalization))
			.ForEach(i => l11nBuilder.BuildTech(i.Key, i.Value));
		return l11nBuilder;
	}

	private static void WriteL11nFragments(ProgressContext ctx, DirectoryInfo outputDirectory) {
		const string fragmentPrefix = $"{nameof(StlTechRelGen)}.Resources.fragments.";
		Assembly assembly = Assembly.GetExecutingAssembly();

		assembly
			.GetManifestResourceNames()
			.Where(i => i.StartsWith(fragmentPrefix, StringComparison.Ordinal))
			.DriveProgressTask(ctx.AddTask(Messages.Progress.WritingLocalizationFragments))
			.Select(i => i[fragmentPrefix.Length..])
			.ForEach(i => {
				using Stream stream = assembly.GetManifestResourceStream(fragmentPrefix + i)!;
				using FileStream file = File.Open(Path.Combine(outputDirectory.FullName, i), GlobalInstances.FileWriteOptions);
				stream.CopyTo(file);
			});
	}

	private static async Task<(string destPath, Mod[] sourceMods)> GetTarget(Config config) {
		using LauncherV2DbContext db = await Inquiries.RunWithProgressAsync(Messages.Progress.ConnectingLauncherDb,
			() => new LauncherV2DbContext(config.Game)
		);

		if (TryRestoreSavedTarget(config, db, out string? destPath, out List<Mod>? mods)) {
			return (destPath, [.. mods]);
		}

		return Inquiries.SelectOutputTarget(config, db);
	}


	private static bool TryRestoreSavedTarget(Config config, LauncherV2DbContext db, [NotNullWhen(true)] out string? destPath, [NotNullWhen(true)] out List<Mod>? mods) {
		destPath = null;
		mods = null;

		if (config.Playset == null) {
			return false;
		}

		if (!Inquiries.ConfirmUseSavedSelection(config)) {
			return false;
		}

		// Vanilla game
		if (string.IsNullOrEmpty(config.Playset.Name)) {
			destPath = config.Playset.Target;
			mods = [];
			return true;
		}

		Playset? playset = db.Playsets.SingleOrDefault(i => i.Name == config.Playset!.Name);
		if (playset == null) {
			LogError(Messages.Error.PlaysetNotFound);
			return false;
		}

		mods = [.. db.GetModsInPlayset(playset)];
		Mod[] targetModCandidates = [.. mods.Where(i => i.DisplayName == config.Playset.Target)];
		if (targetModCandidates.Length == 0) {
			LogError(Messages.Error.TargetModNotFound);
			return false;
		} else if (targetModCandidates.Length > 1) {
			LogError(Messages.Error.MultipleModsSameName);
			LogInfo(Messages.Prompt.CollidingTargetModListHeader);
			foreach (Mod candidate in targetModCandidates) {
				LogInfo(Messages.Prompt.CollidingTargetModListItem.Format(Markup.Escape(candidate.Path())));
			}

			return false;
		}

		Mod targetMod = targetModCandidates[0];
		mods.Remove(targetMod);
		destPath = Path.Combine(targetMod.Path());
		return true;
	}
}
