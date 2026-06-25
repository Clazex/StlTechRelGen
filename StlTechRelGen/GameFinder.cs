using GameFinder.RegistryUtils;
using GameFinder.StoreHandlers.Steam;
using GameFinder.StoreHandlers.GOG;
using GameFinder.Wine;
using GameFinder.Wine.Bottles;

using NexusMods.Paths;

using SteamAppId = GameFinder.StoreHandlers.Steam.Models.ValueTypes.AppId;

namespace StlTechRelGen;

internal static class GameFinder {
	private static readonly SteamAppId ID_STEAM = SteamAppId.From(281990);
	private static readonly GOGGameId ID_GOG = GOGGameId.From(1228928612);
	// We don't know the Xbox Game ID for now
	// private static readonly XboxGameId ID_XBOX = XboxGameId.From("");

	// Returns the discovered game path, or null if discovery failed.
	// A null result serves as the default value for the user-input prompt,
	// allowing the caller to fall back to manual path entry.
	public static string? Find() {
		try {
			return FindNative() ?? FindWine();
		} catch {
			// Silently swallow all failures during game discovery.
			// The caller falls back to prompting the user to enter
			// the path manually, so individual errors (registry reads,
			// Wine prefix enumeration, etc.) are not actionable.
			return null;
		}
	}

	private static string? FindNative() {
		IRegistry? registry = OperatingSystem.IsWindows() ? WindowsRegistry.Shared : null;

		// Steam
		if (new SteamHandler(FileSystem.Shared, registry)
			.FindOneGameById(ID_STEAM, out _) is SteamGame gameSteam
		) {
			return gameSteam.Path.GetFullPath();
		}

		// Windows
		if (registry is not null) {
			// GOG
			if (new GOGHandler(registry, FileSystem.Shared)
				.FindOneGameById(ID_GOG, out _) is GOGGame gameGog
			) {
				return gameGog.Path.GetFullPath();
			}

			// XGP
			/* if (new XboxHandler(FileSystem.Shared)
				.FindOneGameById(ID_XBOX, out _) is XboxGame gameXbox
			) {
				return gameXbox.Path.GetFullPath();
			} */
		}

		return null;
	}

	private static string? FindWine() {
		IEnumerable<AWinePrefix> prefixes = [
			..new DefaultWinePrefixManager(FileSystem.Shared).FindPrefixes()
				.Where(i => i.IsPrefix())
				.Select(i => i.AsPrefix()),
			..new BottlesWinePrefixManager(FileSystem.Shared).FindPrefixes()
				.Where(i => i.IsPrefix())
				.Select(i => i.AsPrefix())
		];

		foreach (AWinePrefix prefix in prefixes) {
			IRegistry registry = prefix.CreateRegistry(FileSystem.Shared);
			IFileSystem fs = prefix.CreateOverlayFileSystem(FileSystem.Shared);

			// GOG Wine
			if (new GOGHandler(registry, fs)
				.FindOneGameById(ID_GOG, out _) is GOGGame gameGog
			) {
				return gameGog.Path.GetFullPath();
			}
		}

		return null;
	}
}
