using Spectre.Console;

namespace StlTechRelGen.Utils;

internal static class LogHelper {
	private static void LogWithColor(Color color, string message) => AnsiConsole.MarkupLine($"[{color.ToMarkup()}]{message}[/]");

	public static void LogInfo(string message) => LogWithColor(Color.Grey, message);

	public static void LogWarning(string message) => LogWithColor(Color.Yellow, message);

	public static void LogError(string message) => LogWithColor(Color.Red, message);
}
