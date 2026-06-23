namespace StlTechRelGen.Utils;

internal static class Constants {
	public static string ConfigPath { get; } = Path.ChangeExtension(Environment.ProcessPath!, "toml");

	// https://stellaris.paradoxwikis.com/Modding#Mod_folder_location
	public static string? DefaultDocumentPath {
		get {
			if (OperatingSystem.IsWindows()) {
				return Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
					@"Paradox Interactive\Stellaris\"
				);
			} else if (OperatingSystem.IsMacOS()) {
				return "~/Documents/Paradox Interactive/Stellaris/";
			} else if (OperatingSystem.IsLinux()) {
				return "~/.local/share/Paradox Interactive/Stellaris/";
			} else {
				return null;
			}
		}
	}

	public static Uri CheckUpdateUrl { get; } =
		new("https://api.github.com/repos/Clazex/stl-tech-rel-gen/releases/latest");

	// Hardcoded in CWTools to identify vanilla dir
	// https://github.com/cwtools/cwtools/blob/b377453dee803f9258be92cfc49896d09039702d/CWTools/Game/Stellaris/STLGame.fs#L551
	public static string GameDirName { get; } = "stellaris";


	public static class L11n {
		public const string Repeatable = "$techrel_repeatable$";
		public const string Requires = "$techrel_requires$";
		public const string Unlocks = "$techrel_unlocks$";
		public const string OneOf = "$techrel_one_of$";

		public const char Times = '×';
		public const string Bullet = "$BULLET_POINT$";
		public const string Mod = "[Mod] ";
		public const string LParen = " (";
		public const char RParen = ')';
		public const char Sep = ' ';
		public const string Dangerous = "$TECH_IS_DANGEROUS$";
		public const string Rare = "$TECH_IS_RARE$";
	}
}
