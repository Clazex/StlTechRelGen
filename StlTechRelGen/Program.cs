using System.Diagnostics.CodeAnalysis;
using System.Text;

using Microsoft.FSharp.Core;

using Spectre.Console;
using Spectre.Console.Cli;

using StlTechRelGen.Db;
using StlTechRelGen.Utils;

using static StlTechRelGen.Lang;
using static StlTechRelGen.Utils.LogHelper;

namespace StlTechRelGen;

public static class Program {
	public static async Task Main(string[] args) {
		try {
			// Add support for codepage 1252, used by CWTools
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

			// Initialize SQLitePCL using the packages:
			// SQLitePCLRaw.config.e_sqlite3 (glue) + SourceGear.sqlite3 (binary).
			SQLitePCL.Batteries_V2.Init();

			Config config;
			if (args.Length > 0) {
				CommandApp<CliCommand> app = new();
				app.Configure(config =>
					config.UseAssemblyInformationalVersion()
				);
				app.Run(args);

				if (CliCommand.Arguments == null) {
					// Main command was not invoked, e.g. -h or -v was used
					return;
				}

				config = CliCommand.Arguments.ToConfig();
			} else {
				config = Config.Load() ?? Inquiries.ConfigTour();
				config.Save(); // Roundtrip to format, or save newly created
			}

			await Run(config);
		} catch (Exception e) {
			AnsiConsole.WriteException(e);
		}
	}

	private static async Task Run(Config config) {
		if (config.OverrideLanguage != null) {
			LoadMessages(config.OverrideLanguage);
		}

		UpdateChecker? updateChecker = config.Update.CheckUpdate ? new UpdateChecker(config.Update) : null;

		if (config.SuppressesCWToolsErrors) {
			// Normally there will be two lines of error complaining missing rule files
			CWTools.Utilities.Utils.logError = FuncConvert.FromAction((string _) => { });
		}

		Inquiries.PromptCloseLauncher(config);

		(Mod targetMod, Mod[] mods) = await GetModsSelection(config);
		DirectoryInfo destDir = new(Path.Combine(targetMod.Path(), "localisation", "replace"));

		try {
			destDir.Delete(true);
		} catch (DirectoryNotFoundException) {
		}

		destDir.Create();

		(int countLocFiles, int countTechs, int countRelations) = await AnsiConsole
			.Progress()
			.UsePreset()
			.StartAsync(async (ctx) => {
				GameData gameData = GameData.LoadWithProgress(ctx, config,
					mods.Select(i => (i.DisplayName!, i.Path()))
				);

				L11nBuilder l11nBuilder = new(gameData);
				gameData.TechTable.Techs
					.DriveProgressTask(ctx.AddTask(Messages.Progress.GeneratingLocalization))
					.ForEach(i => l11nBuilder.BuildTech(i.Key, i.Value));

				l11nBuilder.WriteFilesWithProgress(ctx, destDir.FullName);

				return (
					l11nBuilder.Generated.Keys.Count,
					gameData.TechTable.Techs.Count,
					// On Requires side we'll need to deal with alternatives
					gameData.TechTable.Techs.Values.Sum(i => i.Unlocks.Count) * 2
				);
			});

		AnsiConsole.MarkupLine(Messages.Prompt.SavedLocalization.Format(countLocFiles, destDir.FullName));
		AnsiConsole.MarkupLine(Messages.Prompt.GenerationSummary.Format(countRelations, countTechs));
		if (!config.Yesmen) {
			Inquiries.Pause();
		}

		updateChecker?.TryReport();
	}

	private static async Task<(Mod targetMod, Mod[] mods)> GetModsSelection(Config config) {
		using LauncherV2DbContext db = await Inquiries.ProgressRunAsync(Messages.Progress.ConnectingLauncherDb,
			() => new LauncherV2DbContext(config.Game)
		);

		if (!TryUseSavedPlaysetData(config, db, out Mod? targetMod, out List<Mod>? mods)) {
			Playset playset = Inquiries.ChoosePlayset(db);
			mods = [.. db.GetModsInPlayset(playset)];

			targetMod = Inquiries.ChooseTargetMod(mods);
			mods.Remove(targetMod);

			Inquiries.PromptSavePlaysetData(config, playset.Name, targetMod.DisplayName!);
		}

		return (targetMod, [.. mods]);
	}

	private static bool TryUseSavedPlaysetData(Config config, LauncherV2DbContext db, [NotNullWhen(true)] out Mod? targetMod, [NotNullWhen(true)] out List<Mod>? mods) {
		targetMod = null;
		mods = null;

		if (config.Playset == null) {
			return false;
		}

		if (!Inquiries.ConfirmUseSavedPlaysetData(config)) {
			return false;
		}

		Playset? playset = db.Playsets.SingleOrDefault(i => i.Name == config.Playset!.Name);
		if (playset == null) {
			LogError(Messages.Error.PlaysetNotFound);
			return false;
		}

		mods = [.. db.GetModsInPlayset(playset)];
		Mod[] targetModCandidates = [.. mods.Where(i => i.DisplayName == config.Playset.TargetMod)];
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

		targetMod = targetModCandidates[0];
		mods.Remove(targetMod);
		return true;
	}
}
