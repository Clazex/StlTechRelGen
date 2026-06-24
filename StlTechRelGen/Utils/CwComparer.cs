using static CWTools.Games.Files;

namespace StlTechRelGen.Utils;

internal sealed class CWComparer(WorkspaceDirectory GameDir, WorkspaceDirectoryInput[] ModDirs) : IComparer<CWNode>, IComparer<CWTools.Localisation.Entry>, IComparer<CWRange> {
	public string GamePath => GameDir.path;

	public string[] ModPaths { get; private init; } = [.. ModDirs.Select(
		x => x switch {
			WorkspaceDirectoryInput.WD wd => wd.Item.path,
			WorkspaceDirectoryInput.ZD zd => zd.Item.path,
			_ => throw new NotImplementedException(),
		}
	)];

	public bool IsVanilla(CWRange range) =>
		range.FileName.StartsWith(GamePath, StringComparison.OrdinalIgnoreCase);

	public int ModIndex(CWRange range) => ModPaths.Index()
		.MaxBy((i) => range.FileName.CommonPrefix(i.Item, StringComparison.OrdinalIgnoreCase).Length)
		.Index;

	public int Compare(CWNode? x, CWNode? y) {
		if (x != null && y != null) {
			return Compare(x.Position, y.Position);
		} else if (x != null && y == null) {
			return 1;
		} else if (x == null && y != null) {
			return -1;
		}

		return 0;
	}

	public int Compare(CWTools.Localisation.Entry x, CWTools.Localisation.Entry y) =>
		Compare(x.position, y.position);

	public int Compare(CWRange x, CWRange y) {
		bool isVanillaX = IsVanilla(x);
		bool isVanillaY = IsVanilla(y);
		if (isVanillaX && isVanillaY) {
			return string.Compare(
				x.FileName,
				y.FileName,
				StringComparison.OrdinalIgnoreCase
			);
		} else if (isVanillaX || isVanillaY) {
			return isVanillaX.CompareTo(isVanillaY);
		}

		int modIndexX = ModIndex(x);
		int modIndexY = ModIndex(y);
		return string.Compare(
			x.FileName[ModPaths[modIndexX].Length..],
			y.FileName[ModPaths[modIndexY].Length..],
			StringComparison.OrdinalIgnoreCase
		) switch {
			0 => modIndexX.CompareTo(modIndexY),
			int i => i
		};
	}
}
