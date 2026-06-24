using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

using CWTools.Common;

using Spectre.Console;

using StlTechRelGen.Model;
using StlTechRelGen.Utils;

using static StlTechRelGen.Lang;
using static StlTechRelGen.Utils.LogHelper;

using CWLang = CWTools.Common.Lang;

namespace StlTechRelGen;

internal sealed class L11nBuilder(GameData gameData) {
	public Dictionary<string, Dictionary<string, string>> Generated { get; } = LangHelpers.allSTLLangs.ToDictionary(
		i => i.Name(),
		_ => new Dictionary<string, string>()
	);

	public void BuildTech(string id, Tech tech) {
		StringBuilder sb = new(); // Holds metadata except areas, plus related techs

		if (tech.Dangerous) {
			sb.Append(Constants.L11n.Sep).Append(Constants.L11n.Dangerous);
		}

		if (tech.Rare) {
			sb.Append(Constants.L11n.Sep).Append(Constants.L11n.Rare);
		}

		if (tech.Levels != 1) {
			sb.Append(Constants.L11n.Sep).Append(Constants.L11n.Repeatable);
			if (tech.Levels != -1) {
				sb.Append(Constants.L11n.Times)
					.Append(tech.Levels);
			}
		}

		sb.Append(CultureInfo.InvariantCulture, $"{Constants.L11n.RParen}§!");


		if (tech.Requires.Count > 0) {
			sb.Append($"\n\n{Constants.L11n.Requires}");
			foreach (TechRequirement require in tech.Requires) {
				if (require is TechRequirementSingle single) {
					WriteRelatedTech(sb, single.Id, 1);
				} else if (require is TechRequirementAlternatives alternatives) {
					sb.Append(CultureInfo.InvariantCulture, $"\n$t${Constants.L11n.Bullet}{Constants.L11n.OneOf}");
					foreach (string alternative in alternatives.Alternatives) {
						WriteRelatedTech(sb, alternative, 2);
					}
				} else {
					throw new NotSupportedException();
				}
			}
		}

		if (tech.Unlocks.Count > 0) {
			sb.Append($"\n\n{Constants.L11n.Unlocks}");
			foreach (string unlock in tech.Unlocks) {
				WriteRelatedTech(sb, unlock, 1);
			}
		}

		string contentMain = sb.ToString(); // This includes metadata that's not affected by swaps + relations
		string descKey = $"{id}_desc";

		foreach ((string swapId, TechSwap swapTech) in tech.Swaps.Prepend(new(id, tech.AsSwap()))) {
			// Some metadata might be changed by swaps, generate individual copies for each swap
			string contentSwap = $"\n\n£{swapTech.Area.ToString().ToLowerInvariant()}£"
				+ $" §Y${swapTech.Area.ToString().ToUpperInvariant()}$ "
				+ $"T{tech.Tier}{Constants.L11n.LParen}"
				+ swapTech.Categories.Select(i => $"${i.ToLowerInvariant()}$").Join(Constants.L11n.Sep)
				+ contentMain; // Add up with metadata that are affected by swaps

			foreach ((CWLang lang, ReadOnlyDictionary<string, string> loc) in gameData.Localizations) {
				Dictionary<string, string> dict = Generated[lang.Name()];
				foreach (string suffix in gameData.AuthSuffixes) {
					string descKeySwap = $"{swapId}_desc";
					string descKeySwapFull = $"{descKeySwap}{suffix}";

					if (suffix.Length > 0 && !loc.ContainsKey(descKeySwapFull)) {
						// suffix.Length > 0 => Auth specific desc (Since we always want basic desc)
						// ContainsKey false => This auth has no specific desc
						continue;
					}

					// Currently use Swap desc w/ suffix => Orig desc w/o suffix fallback
					// Not sure if this is correct and covers all cases
					if (!loc.TryGetValue(descKeySwapFull, out string? descOrig)
						&& !loc.TryGetValue(descKey, out descOrig)
					) {
						// Loc not found, use key directly
						// We don't skip directly in order to provide info at best-effort
						descOrig = descKeySwapFull;

						if (descKey == descKeySwapFull) {
							LogWarning(Messages.Data.LocalizationEntryNotFound.Format(descKey));
						} else {
							LogWarning(Messages.Data.LocalizationEntryAndSwapNotFound.Format(descKey, descKeySwapFull));
						}
					}

					// We want to handle the case that this tech's desc contains references of
					// another tech's description
					// Otherwise we'll got [another tech's orig desc] + [another tech's relation info]
					// + [this tech's relation info]
					// We handle this by "resolving" the references and replace them with referenced
					// text which *will* change the loc behaviour in edge cases
					// We don't touch the tech we don't know about, this won't cause issue as long as
					// current generated loc is up-to-date
					string descFinal = GlobalInstances.RegexLocReference().Matches(descOrig)
						.Where(i => gameData.TechTable.AllTechIds.Contains(i.Groups["id"].Value)) // The referenced tech exists
						.Where(i => !i.Groups["suffix"].Success // There is no auth suffix
							|| gameData.AuthSuffixes.Contains(i.Groups["suffix"].Value) // Or the suffix is valid
						)
						.Select(i => i.Groups["key"].Value) // The loc key
						.Distinct() // Deduplicate by loc key
						.Where(loc.ContainsKey) // We know about the loc key
						.Aggregate(
							descOrig, // Starts with original desc
							(desc, i) => desc.Replace($"${i}$", loc[i]), // Resolve each reference
							desc => desc + contentSwap // Add the content part w/ swap
						);

					if (!dict.TryGetValue(descKey, out string? baseDesc) || baseDesc != descFinal) {
						// We don't write if we end up with the same string as the swap-less version
						dict[descKeySwapFull] = descFinal;
					}
				}
			}
		}
	}

	private void WriteRelatedTech(StringBuilder sb, string relTechId, int indent) {
		Tech tech = gameData.TechTable.Techs[relTechId];

		sb.Append("\n$");
		for (int i = 0; i < indent; i++) {
			sb.Append('t');
		}

		sb.Append('$');

		sb.Append(Constants.L11n.Bullet);
		if (!tech.Vanilla) {
			sb.Append(Constants.L11n.Mod);
		}

		sb.Append(CultureInfo.InvariantCulture, $"£{tech.Area.ToString().ToLowerInvariant()}£ ['technology:{relTechId}']");
	}


	public void WriteFilesWithProgress(ProgressContext ctx, string destPath) {
		ProgressTask task = ctx.AddTask(Messages.Progress.WritingLocalization).MaxValue(Generated.Keys.Count);
		Parallel.ForEach(Generated.Keys, (lang) => {
			WriteLangFile(destPath, lang);
			task.Increment(1);
		});
		task.StopTask();
	}

	private void WriteLangFile(string destPath, string lang) {
		Dictionary<string, string> dict = Generated[lang];

		using StreamWriter writer = new(
			Path.Combine(destPath, $"techrel_l_{lang}.yml"),
			GlobalInstances.Utf8Bom,
			GlobalInstances.FileWriteOptions
		);
		writer.AutoFlush = false;
		writer.NewLine = "\n";

		writer.WriteLine($"l_{lang}:");
		foreach ((string key, string value) in dict) {
			// We use partly StringBuilder and partly YamlSerializer here.
			// Since we want:
			//   1. Never quote keys
			//   2. Always quote values
			writer.Write(' ');
			writer.Write(key);
			writer.Write(": ");
			writer.Write(GlobalInstances.Yaml.Serializer.Serialize(value)); // Serializer produces \n
		}
	}
}
