using Tomlyn;
using Tomlyn.Serialization;

namespace StlTechRelGen;

internal sealed partial class Config {
	public sealed class UpdateConfig {
		public bool CheckUpdate { get; set; } = true;

		[TomlIgnore(Condition = TomlIgnoreCondition.WhenWritingDefault)]
		public bool WaitForCheckBeforeExit { get; set; }

		[TomlPropertyName("github_api_token")]
		public string? GitHubApiToken { get; set; }

		[TomlPropertyName("last_etag")]
		public string? LastETag { get; set; }
	}

	public sealed class GameConfig {
		public required string GamePath { get; set; }

		public required string DocumentPath { get; set; }
	}

	public sealed class PlaysetConfig {
		// Use empty string to indicate vanilla
		public required string Name { get; set; }

		// When vanilla, this is output path
		public required string Target { get; set; }
	}

	// Automatically answers "yes" to confirmation prompts where possible,
	// reducing interactivity in automated / batch scenarios. Does not
	// bypass non-y/n prompts such as text input or list selection.
	[TomlIgnore(Condition = TomlIgnoreCondition.WhenWritingDefault)]
	public bool Yesmen { get; set; }

	public string? OverrideLanguage { get; set; }

	[TomlPropertyName("suppresses_cwtools_errors")]
	public bool SuppressesCWToolsErrors { get; set; } = true;

	public UpdateConfig Update { get; set; } = new();
	public required GameConfig Game { get; set; }
	public PlaysetConfig? Playset { get; set; }


	internal static Config? Load() {
		try {
			using FileStream file = File.OpenRead(Constants.ConfigPath);
			return TomlSerializer.Deserialize(file, TomlContext.Default.Config)!;
		} catch (FileNotFoundException) {
			return null;
		} catch (TomlException e) {
			LogError(Messages.Error.ConfigParseFailed);
			AnsiConsole.WriteException(e, ExceptionFormats.NoStackTrace);
			LogWarning(Messages.Prompt.GenerateNewConfig);
			AnsiConsole.WriteLine();
			return null;
		}
	}

	internal void Save() {
		using FileStream file = File.OpenWrite(Constants.ConfigPath);
		TomlSerializer.Serialize(file, this, TomlContext.Default.Options with {
			StringStylePreferences = new() {
				DefaultStyle = TomlStringStyle.Literal
			}
		});
	}
}
