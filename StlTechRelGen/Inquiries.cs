using System.Diagnostics;

using StlTechRelGen.Db;

using static Spectre.Console.AnsiConsole;

namespace StlTechRelGen;

internal static class Inquiries {
	private const string LAUNCHER_PROC_NAME = "paradox launcher";

	// Workaround for collision between class Spectre.Console.Markup
	// and method Spectre.Console.AnsiConsole.Markup
	private static string Escape(string text) =>
		Spectre.Console.Markup.Escape(text);

	internal static void Pause() {
		Prompt(new TextPrompt<string>(Messages.Prompt.Pause)
			.Secret(null).AllowEmpty().ClearOnFinish(true)
		);
		WriteLine();
	}

	public static async Task<T> RunWithProgressAsync<T>(
		string desc, Func<T> func
	) => await Progress()
		.UsePreset()
		.StartAsync(async (ctx) => RunWithProgress(ctx, desc, func));

	public static T RunWithProgress<T>(
		ProgressContext ctx, string desc, Func<T> func
	) {
		ProgressTask task = ctx.AddTask(desc).IsIndeterminate().MaxValue(1);
		T result = func();
		task.Value(1).StopTask();
		return result;
	}

	internal static void WaitForLauncherClose(Config config) {
		static bool IsLauncherOpen() => Process.GetProcesses()
			.Any(i => i.ProcessName.Equals(
				LAUNCHER_PROC_NAME,
				StringComparison.OrdinalIgnoreCase
			));

		if (IsLauncherOpen()) {
			MarkupLine(Messages.Prompt.CloseLauncher);
		} else {
			return;
		}

		do {
			if (config.Yesmen) {
				Thread.Sleep(1000);
			} else {
				Pause();
			}
		} while (IsLauncherOpen());
	}

	internal static bool ConfirmUseSavedSelection(Config config) {
		if (config.Yesmen) {
			return true;
		}

		return Confirm(
			(string.IsNullOrEmpty(config.Playset!.Name)
				? Messages.Prompt.ConfirmUseSavedVanilla
				: Messages.Prompt.ConfirmUseSavedTarget
			).Format(
				Escape(config.Playset!.Name),
				Escape(config.Playset.Target)
			)
		);
	}

	internal static (string destPath, Mod[] mods) SelectOutputTarget(
		Config config,
		LauncherV2DbContext db
	) {
		List<Mod> mods;
		string destPath;

		Playset playset = SelectPlayset(db);
		if (string.IsNullOrEmpty(playset.Name)) {
			destPath = PromptOutputPath();
			mods = [];
			SaveTargetIfConfirmed(config, playset.Name, destPath);
			return (destPath, [.. mods]);
		}

		mods = [.. db.GetModsInPlayset(playset)];

		Mod targetMod = SelectTargetMod(mods);
		mods.Remove(targetMod);
		destPath = targetMod.Path();

		SaveTargetIfConfirmed(config, playset.Name, targetMod.DisplayName!);
		return (destPath, [.. mods]);
	}

	private static Playset SelectPlayset(LauncherV2DbContext db) =>
		Prompt(new SelectionPrompt<Playset>()
			.UsePreset()
			.Title(Messages.Prompt.ChoosePlayset)
			.AddChoices(db.Playsets.OrderByDescending(i => i.IsActive))
			.AddCancelResult(new Playset() { Name = "", IsActive = false })
			.UseConverter(i => i.IsActive ?? false
				? Messages.Prompt.CurrentPlaysetPrefix + Escape(i.Name)
				: Escape(i.Name)
			)
		);

	internal static string PromptOutputPath() =>
		Path.GetFullPath(Ask<string>(Messages.Prompt.AskOutputPath));

	internal static Mod SelectTargetMod(IEnumerable<Mod> mods) =>
		Prompt(new SelectionPrompt<Mod>()
			.UsePreset()
			.Title(Messages.Prompt.ChooseTargetMod)
			.AddChoices(mods)
			.PageSize(8)
			.EnableSearch()
			.UseConverter(i =>
				$"[white]{Escape(i.DisplayName!)}[/]"
				+ $" [dim]({Escape(i.Path())})[/]"
			)
		);

	internal static void SaveTargetIfConfirmed(
		Config config,
		string playsetName,
		string targetModName
	) {
		if (!config.Yesmen && !Confirm((
			string.IsNullOrEmpty(playsetName)
				? Messages.Prompt.ConfirmSaveVanilla
				: Messages.Prompt.ConfirmSaveTarget
			).Format(
				Escape(playsetName),
				Escape(targetModName)
			)
		)) {
			return;
		}

		config.Playset = new() {
			Name = playsetName,
			Target = targetModName
		};
		config.Save();
	}

	internal static Config RunSetupWizard() {
		MarkupLine(Messages.Prompt.Welcome);
		MarkupLine(Messages.Prompt.ReadInstructions);
		Pause();

		Config config = new() {
			Game = new() {
				GamePath = "",
				DocumentPath = ""
			}
		};

		// Game

		TextPrompt<string> promptGamePath = new(Messages.Prompt.AskGamePath);
		if (GameFinder.Find() is string foundPath) {
			promptGamePath.DefaultValue(foundPath);
		}

		config.Game.GamePath = Path.GetFullPath(Prompt(promptGamePath));

		TextPrompt<string> promptDocumentPath = new(Messages.Prompt.AskDocumentPath);
		if (Constants.DefaultDocumentPath is string defaultDocumentPath) {
			promptDocumentPath.DefaultValue(Path.GetFullPath(defaultDocumentPath));
		}

		config.Game.DocumentPath = Path.GetFullPath(Prompt(promptDocumentPath));

		MarkupLine(Messages.Prompt.ConfigCreated);

		return config;
	}
}
