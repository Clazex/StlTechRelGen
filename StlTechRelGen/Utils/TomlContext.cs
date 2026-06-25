using System.Text.Json.Serialization;

using Tomlyn.Serialization;

namespace StlTechRelGen.Utils;

[TomlSourceGenerationOptions(
	PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower
// Converters = [typeof(CompositeFormatConverter)]
)]
[TomlSerializable(typeof(Config))]
// Lang uses reflection-based deserialization because the Tomlyn source
// generator cannot handle custom converters (CompositeFormatConverter).
// See https://github.com/xoofx/Tomlyn/issues/132
// [TomlSerializable(typeof(Lang))]
internal sealed partial class TomlContext : TomlSerializerContext {
	internal sealed class CompositeFormatConverter : TomlConverter<CompositeFormat> {
		public override CompositeFormat? Read(TomlReader reader) =>
			CompositeFormat.Parse(reader.GetString());

		public override void Write(TomlWriter writer, CompositeFormat value) =>
			writer.WriteStringValue(value.Format);
	}
}
