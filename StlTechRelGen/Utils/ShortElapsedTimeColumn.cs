using Spectre.Console;
using Spectre.Console.Rendering;

namespace StlTechRelGen.Utils;

/// <summary>
/// A column showing the elapsed time of a task. Basically copy-pasted from ElapsedTimeColumn.
/// </summary>
public sealed class ShortElapsedTimeColumn : ProgressColumn {
	/// <inheritdoc/>
	protected override bool NoWrap => true;

	/// <summary>
	/// Gets or sets the style of the remaining time text.
	/// </summary>
	public Style Style { get; set; } = Color.Blue;

	/// <inheritdoc/>
	public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime) {
		TimeSpan? elapsed = task.ElapsedTime;
		if (elapsed == null) {
			return new Markup("--:--");
		}

		if (elapsed.Value.TotalMinutes > 99) {
			return new Markup("**:**");
		}

		return new Text($"{elapsed.Value:mm\\:ss}", Style);
	}

	/// <inheritdoc/>
	public override int? GetColumnWidth(RenderOptions options) => "**:**".Length;
}
