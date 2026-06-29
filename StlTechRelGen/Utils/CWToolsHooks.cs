using CWTools.Games;

using HarmonyLib;

namespace StlTechRelGen.Utils;

internal static class CWToolsHooks {
	private static readonly Lazy<Harmony> harmony = new(() =>
		Harmony.CreateAndPatchAll(
		typeof(CWToolsHooks),
		$"{nameof(StlTechRelGen)}.{nameof(CWToolsHooks)}"
		)
	);

	internal static void Init() => _ = harmony.Value;

	internal static Action? OnProgress;

#pragma warning disable IDE0051 // Remove unused private members

	[HarmonyPatch(typeof(ResourceManager<SECData>), "updateFiles")]
	[HarmonyPatch(typeof(LocalisationManager<SECData>), "updateAllLocalisationSources")]
	[HarmonyPatch(typeof(LocalisationManager<SECData>), "updateProcessedLocalisation")]
	[HarmonyPostfix]
	private static void Progression() =>
		OnProgress?.Invoke();

#pragma warning restore IDE0051 // Remove unused private members
}
