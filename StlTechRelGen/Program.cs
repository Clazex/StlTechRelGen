using System.Diagnostics;
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

		// Spectre.Console.Cli normally runs a command to completion inside
		// app.Run(). We hijack Execute() to capture parsed arguments into
		// a static property, then return control to the caller.
		// If Arguments is null, a built-in option (-h/-v) ran instead;
		// for those options we call Environment.Exit and never return.
		CommandApp<CliArgs> app = new();
		app.Configure(config =>
			config.UseAssemblyInformationalVersion()
		);
		app.Run(args);

		if (CliArgs.Arguments is not CliArgs.Settings arguments) {
			// Main command was not invoked, e.g. -h or -v was used
			Environment.Exit(0);
			throw new UnreachableException();
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
			// CWTools emits two error lines about missing rule files.
			// Suppress them by replacing the F# logError function with a
			// no-op. FuncConvert.FromAction bridges C# Action to FSharpFunc.
			CWTools.Utilities.Utils.logError = FuncConvert.FromAction((string _) => { });
		}

		Inquiries.WaitForLauncherClose(config);

		(string destPath, Mod[] mods) = await GetTarget(config);
		DirectoryInfo l10nDir = new(Path.Combine(destPath, "localisation"));
		PrepareOutputDir(l10nDir);

		(int countL10nFiles, int countTechs, int countRelations) = await AnsiConsole
			.Progress()
			.UsePreset()
			.StartAsync(async (ctx) => {
				GameData gameData = LoadGameData(ctx, config, mods);
				L10nBuilder l10nBuilder = BuildL10n(ctx, gameData);

				Parallel.Invoke(
					() => WriteL10nFragments(ctx, l10nDir),
					() => l10nBuilder.WriteFilesWithProgress(
						ctx,
						l10nDir.CreateSubdirectory("replace").FullName
					)
				);

				return (
					l10nBuilder.Generated.Keys.Count,
					gameData.TechTable.Techs.Count,
					// 2 * because each Unlocks entry represents a
					// bidirectional edge: "tech_a requires tech_b"
					// produces both a "requires" line and an "unlocks"
					// line in the output.
					2 * gameData.TechTable.Techs.Values
						.Sum(i => i.Unlocks.Count)
				);
			});

		Log(Messages.Prompt.SavedL10n, countL10nFiles, Markup.Escape(l10nDir.FullName));
		Log(Messages.Prompt.GenerationSummary, countRelations, countTechs);
		if (!config.Yesmen) {
			Inquiries.Pause();
		}

		updateChecker?.TryReport();
	}


	private static void PrepareOutputDir(DirectoryInfo outputDir) {
		try {
			if (outputDir.Exists) {
				outputDir.Delete(true);
			}

			outputDir.Create();
		} catch {
			LogError(
				Messages.Error.FailedToAccessOutputDir,
				Markup.Escape(outputDir.FullName)
			);
			throw;
		}
	}

	private static GameData LoadGameData(
		ProgressContext ctx,
		Config config,
		Mod[] sourceMods
	) => GameData.LoadWithProgress(
		ctx, config, sourceMods.Select(i => (i.DisplayName!, i.Path()))
	);

	private static L10nBuilder BuildL10n(ProgressContext ctx, GameData gameData) {
		L10nBuilder l10nBuilder = new(gameData);
		gameData.TechTable.Techs
			.DriveProgressTask(ctx.AddTask(Messages.Progress.GeneratingL10n))
			.ForEach(i => l10nBuilder.BuildTech(i.Key, i.Value));
		return l10nBuilder;
	}

	private static void WriteL10nFragments(
		ProgressContext ctx,
		DirectoryInfo outputDirectory
	) {
		const string fragmentPrefix = $"{nameof(StlTechRelGen)}.Resources.fragments.";
		Assembly assembly = Assembly.GetExecutingAssembly();

		assembly
			.GetManifestResourceNames()
			.Where(i => i.StartsWith(fragmentPrefix, StringComparison.Ordinal))
			.DriveProgressTask(ctx.AddTask(Messages.Progress.WritingL10nFragments))
			.Select(i => i[fragmentPrefix.Length..])
			.ForEach(i => {
				using Stream stream = assembly
					.GetManifestResourceStream(fragmentPrefix + i)!;
				using FileStream file = File.Open(
					Path.Combine(outputDirectory.FullName, i),
					GlobalInstances.FileWriteOptions
				);
				stream.CopyTo(file);
			});
	}

	private static async Task<(string destPath, Mod[] sourceMods)> GetTarget(
		Config config
	) {
		using LauncherV2DbContext db = await Inquiries.RunWithProgressAsync(
			Messages.Progress.ConnectingLauncherDb,
			() => new LauncherV2DbContext(config.Game)
		);

		if (TryRestoreSavedTarget(
			config, db, out string? destPath, out List<Mod>? mods
		)) {
			return (destPath, [.. mods]);
		}

		return Inquiries.SelectOutputTarget(config, db);
	}


	private static bool TryRestoreSavedTarget(
		Config config,
		LauncherV2DbContext db,
		[NotNullWhen(true)] out string? destPath,
		[NotNullWhen(true)] out List<Mod>? mods
	) {
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

		Playset? playset = db.Playsets
			.SingleOrDefault(i => i.Name == config.Playset!.Name);
		if (playset == null) {
			LogError(Messages.Error.PlaysetNotFound);
			return false;
		}

		mods = [.. db.GetModsInPlayset(playset)];
		Mod[] targetModCandidates = [.. mods
			.Where(i => i.DisplayName == config.Playset.Target)];
		if (targetModCandidates.Length == 0) {
			LogError(Messages.Error.TargetModNotFound);
			return false;
		} else if (targetModCandidates.Length > 1) {
			LogError(Messages.Error.MultipleModsSameName);
			Log(Messages.Prompt.TargetModCollisionHeader);
			foreach (Mod candidate in targetModCandidates) {
				Log(
					Messages.Prompt.TargetModCollisionItem,
					Markup.Escape(candidate.Path())
				);
			}

			return false;
		}

		Mod targetMod = targetModCandidates[0];
		mods.Remove(targetMod);
		destPath = Path.Combine(targetMod.Path());
		return true;
	}
}
