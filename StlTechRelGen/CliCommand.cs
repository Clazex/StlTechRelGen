using System.ComponentModel;

using Spectre.Console.Cli;

using StlTechRelGen.Utils;

namespace StlTechRelGen;

internal sealed class CliCommand : Command<CliCommand.CliArguments> {
	internal sealed class CliArguments : CommandSettings {
		[CommandOption("-g|--game-path", isRequired: true)]
		[Description("Path to the game")]
		public required string GamePath { get; init; }

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
				GamePath = GamePath,
				DocumentPath = DocumentPath ?? Constants.DefaultDocumentPath ??
					throw new NotSupportedException("Default document path is not available for this OS"),
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

	internal static CliArguments? Arguments { get; private set; }

	// We only utilize argument parsing feature here
	protected override int Execute(CommandContext context, CliArguments arguments, CancellationToken cancellationToken) {
		Arguments = arguments;
		return 0;
	}
}
