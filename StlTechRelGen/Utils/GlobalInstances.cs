using System.Text.RegularExpressions;

namespace StlTechRelGen.Utils;

internal static partial class GlobalInstances {
	public static class Yaml {
		internal static YamlDeserializer Deserializer { get; } = new();

		internal static IYamlSerializer Serializer { get; } = new YamlSerializerBuilder()
			.WithDefaultScalarStyle(YamlDotNet.Core.ScalarStyle.DoubleQuoted)
			.WithNewLine("\n")
			.Build();
	}

	internal static Encoding Utf8Bom { get; } = new UTF8Encoding(
		encoderShouldEmitUTF8Identifier: true,
		throwOnInvalidBytes: true
	);

	internal static HttpClient Client { get; } = new();

	[GeneratedRegex(
		@"^\$(?<key>(?<id>.+)_desc(?<suffix>_.+)?)\$$",
		RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant
	)]
	internal static partial Regex RegexLocReference();

	internal static readonly FileStreamOptions FileWriteOptions = new() {
		Mode = FileMode.Create,
		Access = FileAccess.Write,
		Share = FileShare.None,
		Options = FileOptions.SequentialScan
	};

	static GlobalInstances() =>
		Client.DefaultRequestHeaders.UserAgent.Add(new(
			nameof(StlTechRelGen),
			GetProgramVersion()
		));

	public static string GetProgramVersion() => "v" + Assembly
		.GetExecutingAssembly()
		.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
		.InformationalVersion!;
}
