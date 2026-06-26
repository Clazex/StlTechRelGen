using System.ComponentModel;

using Spectre.Console.Cli;

namespace StlTechRelGen;

internal sealed class CliArgs : Command<CliArgs.Settings> {
	internal sealed class Settings : CommandSettings {
		[CommandOption("-g|--game-path")]
		[Description("Path to the game")]
		public string? GamePath { get; init; }

		[CommandOption("-d|--document-path")]
		[Description("Path to the document")]
		public string? DocumentPath { get; init; }

		[CommandOption("-p|--playset", isRequired: true)]
		[Description("Playset to use")]
		public required string Playset { get; init; }

		[CommandOption("-m|--target-mod", isRequired: true)]
		[Description("Target mod to write localization to")]
		public required string TargetMod { get; init; }

		[CommandOption("--no-check-update")]
		[Description("Don't check for updates")]
		public bool NoCheckUpdate { get; init; } = false;

		public Config ToConfig() => new() {
			Yesmen = true,
			Game = new Config.GameConfig {
				GamePath = GamePath ?? GameFinder.Find()
					?? throw new NotSupportedException(
						"Game path is not provided and failed to be found automatically"
					),
				DocumentPath = DocumentPath ?? Constants.DefaultDocumentPath
					?? throw new NotSupportedException(
						"Document path is not provided and cannot be determined automatically for this OS"
					),
			},
			Playset = new Config.PlaysetConfig {
				Name = Playset,
				Target = TargetMod,
			},
			Update = new Config.UpdateConfig {
				CheckUpdate = !NoCheckUpdate,
			},
		};
	}

	internal static Settings? Arguments { get; private set; }

	// We only utilize argument parsing feature here
	protected override int Execute(CommandContext context, Settings arguments, CancellationToken cancellationToken) {
		Arguments = arguments;
		return 0;
	}
}
