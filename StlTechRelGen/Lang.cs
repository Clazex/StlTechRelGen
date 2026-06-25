using System.Text.Json;

using Tomlyn;

namespace StlTechRelGen;

internal sealed class Lang {
	private const string DEFAULT_LANG = "en";

	internal static Lang Messages { get; private set; } = null!;

	static Lang() => LoadMessages(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch {
		"en" => "en",
		"zh" => "zh",
		_ => DEFAULT_LANG
	});

	internal static void LoadMessages(string? lang) => Messages = TomlSerializer.Deserialize<Lang>(
		Assembly.GetExecutingAssembly()
			.GetManifestResourceStream($"{nameof(StlTechRelGen)}.Resources.lang.{lang}.toml")!,
		// TomlContext.Default // Source generation has some issues with converters (https://github.com/xoofx/Tomlyn/issues/132)
		new TomlSerializerOptions() { // Use reflection-based for now
			PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
			Converters = [new TomlContext.CompositeFormatConverter()]
		}
	)!;

	public required PromptMessages Prompt { get; init; }
	public required ProgressMessages Progress { get; init; }
	public required DataMessages Data { get; init; }
	public required UpdateMessages Update { get; init; }
	public required ErrorMessages Error { get; init; }


	internal sealed class PromptMessages {
		public required string SearchPlaceholderText { get; init; }
		public required string MoreChoicesText { get; init; }

		public required string GenerateNewConfig { get; init; }

		public required string Welcome { get; init; }
		public required string ReadInstructions { get; init; }
		public required string Pause { get; init; }

		public required string AskGamePath { get; init; }
		public required string AskDocumentPath { get; init; }
		public required string ConfigCreated { get; init; }

		public required string CloseLauncher { get; init; }
		public required CompositeFormat ConfirmUseSavedVanilla { get; init; }
		public required CompositeFormat ConfirmUseSavedTarget { get; init; }
		public required string ChoosePlayset { get; init; }
		public required string CurrentPlaysetPrefix { get; init; }
		public required string AskOutputPath { get; init; }
		public required string ChooseTargetMod { get; init; }
		public required string CollidingTargetModListHeader { get; init; }
		public required CompositeFormat CollidingTargetModListItem { get; init; }
		public required CompositeFormat ConfirmSaveVanilla { get; init; }
		public required CompositeFormat ConfirmSaveTarget { get; init; }

		public required CompositeFormat SavedLocalization { get; init; }
		public required CompositeFormat GenerationSummary { get; init; }
	}

	internal sealed class ProgressMessages {
		public required string ConnectingLauncherDb { get; init; }
		public required string LoadingGame { get; init; }
		public required string LoadingScriptedVariables { get; init; }
		public required string LoadingAuthoritySuffixes { get; init; }
		public required string LoadingTechnologies { get; init; }
		public required string BuildingTechRelationship { get; init; }
		public required string LoadingLocalization { get; init; }
		public required string GeneratingLocalization { get; init; }
		public required string WritingLocalizationFragments { get; init; }
		public required string WritingLocalization { get; init; }
	}

	internal sealed class DataMessages {
		public required CompositeFormat LocalizationEntryNotFound { get; init; }
		public required CompositeFormat LocalizationEntryAndSwapNotFound { get; init; }
		public required CompositeFormat TechUnknownPrereq { get; init; }
		public required CompositeFormat TechUnnamedSwap { get; init; }
		public required CompositeFormat LocalizationCycleReference { get; init; }
	}

	internal sealed class UpdateMessages {
		public required CompositeFormat NewVersionFound { get; init; }
		public required CompositeFormat ReleaseBody { get; init; }
	}

	internal sealed class ErrorMessages {
		public required string ConfigParseFailed { get; init; }
		public required string PlaysetNotFound { get; init; }
		public required string TargetModNotFound { get; init; }
		public required string MultipleModsSameName { get; init; }
		public required CompositeFormat FailedToAccessOutputDir { get; init; }
	}
}
