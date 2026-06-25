using StlTechRelGen.Model;

namespace StlTechRelGen;

internal sealed class L10nBuilder(GameData gameData) {
	public Dictionary<string, Dictionary<string, string>> Generated { get; } = LangHelpers.allSTLLangs.ToDictionary(
		i => i.Name(),
		_ => new Dictionary<string, string>()
	);

	public void BuildTech(string id, Tech tech) {
		StringBuilder sb = new(); // Holds metadata except areas, plus related techs

		if (tech.Dangerous) {
			sb.Append(Constants.L10n.Sep).Append(Constants.L10n.Dangerous);
		}

		if (tech.Rare) {
			sb.Append(Constants.L10n.Sep).Append(Constants.L10n.Rare);
		}

		if (tech.Levels != 1) {
			sb.Append(Constants.L10n.Sep).Append(Constants.L10n.Repeatable);
			if (tech.Levels != -1) {
				sb.Append(Constants.L10n.Times)
					.Append(tech.Levels);
			}
		}

		sb.Append(CultureInfo.InvariantCulture, $"{Constants.L10n.RParen}§!");


		if (tech.Requires.Count > 0) {
			sb.Append($"\n\n{Constants.L10n.Requires}");
			foreach (TechRequirement require in tech.Requires) {
				if (require is TechRequirementSingle single) {
					WriteRelatedTech(sb, single.Id, 1);
				} else if (require is TechRequirementAlternatives alternatives) {
					sb.Append(CultureInfo.InvariantCulture, $"\n$t${Constants.L10n.Bullet}{Constants.L10n.OneOf}");
					foreach (string alternative in alternatives.Alternatives) {
						WriteRelatedTech(sb, alternative, 2);
					}
				} else {
					throw new NotSupportedException();
				}
			}
		}

		if (tech.Unlocks.Count > 0) {
			sb.Append($"\n\n{Constants.L10n.Unlocks}");
			foreach (string unlock in tech.Unlocks) {
				WriteRelatedTech(sb, unlock, 1);
			}
		}

		string contentMain = sb.ToString(); // This includes metadata that's not affected by swaps + relations
		string descKey = $"{id}_desc";

		// Treat the base tech as a swap of itself so the base entry and every real
		// swap entry share one generation path. AsSwap() copies only area, categories,
		// and vanilla — the three properties a technology_swap block may override.
		foreach ((string swapId, TechSwap swapTech) in tech.Swaps.Prepend(new(id, tech.AsSwap()))) {
			// Some metadata might be changed by swaps, generate individual copies for each swap
			string contentSwap = $"\n\n£{swapTech.Area.ToString().ToLowerInvariant()}£"
				+ $" §Y${swapTech.Area.ToString().ToUpperInvariant()}$ "
				+ $"T{tech.Tier}{Constants.L10n.LParen}"
				+ swapTech.Categories.Select(i => $"${i.ToLowerInvariant()}$").Join(Constants.L10n.Sep)
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

					// A tech description may reference another tech's description via loc keys.
					// Without resolving these references, the output incorrectly nests the
					// referenced tech's relation info inside the current tech's description:
					//   [referenced tech's orig desc] + [referenced tech's relation info]
					//   + [this tech's relation info]
					// We resolve by replacing loc key references with the referenced tech's
					// own original description (which doesn't include its relation info).
					// Note: this changes loc behavior in edge cases (nested references).
					// Unknown techs are left as-is; this is safe as long as generated loc
					// is kept up-to-date.
					string descFinal = GlobalInstances.RegexLocReference()
						.Matches(descOrig)
						// 1. Only resolve references to techs we actually know about
						.Where(i => gameData.TechTable.AllTechIds.Contains(i.Groups["id"].Value))
						// 2. Skip auth-suffixed references pointing to non-existing suffixes
						.Where(i => !i.Groups["suffix"].Success
							|| gameData.AuthSuffixes.Contains(i.Groups["suffix"].Value)
						)
						// 3. Extract the full loc key (e.g., "tech_foo_desc")
						.Select(i => i.Groups["key"].Value)
						// 4. Deduplicate — multiple references may resolve to the same key
						.Distinct()
						// 5. Skip loc keys we have no localization for (guard for missing data)
						.Where(loc.ContainsKey)
						// 6. Replace each $key$ in descOrig with the resolved text,
						//    then append the swap-specific content payload
						.Aggregate(
							descOrig,
							(desc, i) => desc.Replace($"${i}$", loc[i]),
							desc => desc + contentSwap
						);

					// Skip if the final text is identical to the base (non-swap) entry.
					// This commonly happens when authority-specific suffixes produce
					// the same resolved description — writing duplicates would bloat output.
					if (!dict.TryGetValue(descKey, out string? baseDesc) || baseDesc != descFinal) {
						dict[descKeySwapFull] = descFinal;
					}
				}
			}
		}
	}

	private void WriteRelatedTech(StringBuilder sb, string relTechId, int indent) {
		if (!gameData.TechTable.Techs.TryGetValue(relTechId, out Tech? tech)) {
			// Warning is handled by TechTable, so we simply skip here.
			return;
		}

		sb.Append("\n$");
		for (int i = 0; i < indent; i++) {
			sb.Append('t');
		}

		sb.Append('$');

		sb.Append(Constants.L10n.Bullet);
		if (!tech.Vanilla) {
			sb.Append(Constants.L10n.Mod);
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
