using System.Diagnostics;

using Spectre.Console;

using StlTechRelGen.Db;
using StlTechRelGen.Utils;

using static Spectre.Console.AnsiConsole;

using static StlTechRelGen.Lang;

namespace StlTechRelGen;

internal static class Inquiries {
	// Workaround for collision between class Spectre.Console.Markup and method Spectre.Console.AnsiConsole.Markup
	private static string Escape(string text) => Spectre.Console.Markup.Escape(text);

	internal static void Pause() {
		Prompt(new TextPrompt<string>(Messages.Prompt.Pause)
			.Secret(null).AllowEmpty().ClearOnFinish(true)
		);
		WriteLine();
	}

	public static async Task<T> ProgressRunAsync<T>(string desc, Func<T> func) => await Progress()
		.UsePreset()
		.StartAsync(async (ctx) => ProgressRun(ctx, desc, func));

	public static T ProgressRun<T>(ProgressContext ctx, string desc, Func<T> func) {
		ProgressTask task = ctx.AddTask(desc).IsIndeterminate().MaxValue(1);
		T result = func();
		task.Value(1).StopTask();
		return result;
	}

	internal static void PromptCloseLauncher(Config config) {
		static bool IsLauncherOpen() => Process.GetProcesses()
			.Any(i => i.ProcessName.Equals("paradox launcher", StringComparison.OrdinalIgnoreCase));

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

	internal static bool ConfirmUseSavedTarget(Config config) {
		if (config.Yesmen) {
			return true;
		}

		return Confirm(
			(string.IsNullOrEmpty(config.Playset!.Name)
				? Messages.Prompt.ConfirmUseSavedVanilla
				: Messages.Prompt.ConfirmUseSavedTarget
			).Format(
				Escape(config.Playset!.Name), Escape(config.Playset.Target)
			)
		);
	}

	internal static Playset ChoosePlayset(LauncherV2DbContext db) => Prompt(new SelectionPrompt<Playset>()
		.UsePreset()
		.Title(Messages.Prompt.ChoosePlayset)
		.AddChoices(db.Playsets.OrderByDescending(i => i.IsActive))
		.AddCancelResult(new Playset() { Name = "", IsActive = false })
		.UseConverter(i => i.IsActive ?? false
			? Messages.Prompt.CurrentPlaysetPrefix + Escape(i.Name)
			: Escape(i.Name)
		)
	);

	internal static string ChooseOutputPath() =>
		Path.GetFullPath(Ask<string>(Messages.Prompt.AskOutputPath));

	internal static Mod ChooseTargetMod(IEnumerable<Mod> mods) => Prompt(new SelectionPrompt<Mod>()
		.UsePreset()
		.Title(Messages.Prompt.ChooseTargetMod)
		.AddChoices(mods)
		.PageSize(8)
		.EnableSearch()
		.UseConverter(i => $"[white]{Escape(i.DisplayName!)}[/] [dim]({Escape(i.Path())}[/])")
	);

	internal static void PromptSaveTarget(Config config, string playsetName, string targetModName) {
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

	internal static Config ConfigTour() {
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

		config.Game.GamePath = Path.GetFullPath(Ask<string>(Messages.Prompt.AskGamePath));
		if (Constants.DefaultDocumentPath is string defaultDocumentPath) {
			string defaultPath = Path.GetFullPath(defaultDocumentPath);

			MarkupLine(Messages.Prompt.InferredDocumentPath.Format(defaultDocumentPath));

			config.Game.DocumentPath = Confirm(Messages.Prompt.ConfirmDocumentPath)
				? defaultPath
				: Path.GetFullPath(Prompt(
					new TextPrompt<string>(Messages.Prompt.AskDocumentPath)
						.DefaultValue(defaultPath)
				));
		} else {
			MarkupLine(Messages.Prompt.CannotInferDocumentPath);
			config.Game.DocumentPath = Path.GetFullPath(Ask<string>(Messages.Prompt.AskDocumentPath));
		}

		MarkupLine(Messages.Prompt.ConfigCreated);

		return config;
	}
}
