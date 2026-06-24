namespace StlTechRelGen.Model;

internal sealed class Tech(
	bool vanilla,
	TechArea area,
	int tier,
	List<string> categories,
	int levels,
	bool dangerous,
	bool rare,
	List<TechRequirement> requires,
	Dictionary<string, TechSwap> swaps
) {
	public bool Vanilla { get; private init; } = vanilla;
	public TechArea Area { get; private init; } = area;
	public int Tier { get; private init; } = tier;
	public List<string> Categories { get; private init; } = categories;

	public int Levels { get; private init; } = levels;
	public bool Dangerous { get; private init; } = dangerous;
	public bool Rare { get; private init; } = rare;

	public List<TechRequirement> Requires { get; private init; } = requires;
	public List<string> Unlocks { get; private init; } = [];
	public Dictionary<string, TechSwap> Swaps { get; private init; } = swaps;

	public TechSwap AsSwap() => new(Vanilla, Area, Categories);

	public static Tech Parse(CWNode node, CWComparer cwComparer, IReadOnlyDictionary<string, CWValue> variables) {
		bool vanilla = cwComparer.IsVanilla(node.Position);
		TechArea area = Enum.Parse<TechArea>(node.Tag("area").Value.ToRawString(), true);

		string tierStr = node.Tag("tier").Value.ToRawString();
		if (!int.TryParse(tierStr, out int tier)) {
			if (!tierStr.StartsWith('@')) {
				throw new NotSupportedException($"Unexpected tier value: {tierStr}");
			}

			tier = ((CWValue.Int) variables[tierStr]).Item;
		}

		List<string> categories = [.. node.Child("category").Value.LeafValues.Select(i => i.ValueText)];

		int levels = node.Tag("levels")
			.Map(x => {
				string str = x.ToRawString();
				if (int.TryParse(str, out int levels)) {
					return levels;
				}

				if (!str.StartsWith('@')) {
					throw new NotSupportedException($"Unexpected levels value: {str}");
				}

				return ((CWValue.Int) variables[str]).Item;
			})
			.UnwrapOr(1);

		bool dangerous = node.Tag("is_dangerous")
			.Map(x => x.ToRawString().Equals("yes", StringComparison.OrdinalIgnoreCase))
			.UnwrapOr(false);
		bool rare = node.Tag("is_rare")
			.Map(x => x.ToRawString().Equals("yes", StringComparison.OrdinalIgnoreCase))
			.UnwrapOr(false);

		List<TechRequirement> requires = node.Child("prerequisites")
			.Map(x => {
				List<TechRequirement> list = [.. x.LeafValues.Select(x => new TechRequirementSingle(x.Value.ToRawString()))];

				foreach (CWNode clause in x.Children) {
					if (!string.Equals(clause.Key, "OR", StringComparison.OrdinalIgnoreCase)) {
						throw new NotSupportedException();
					}

					list.Add(new TechRequirementAlternatives(
						clause.LeafValues.Select(i => i.Value.ToRawString())
					));
				}

				return list;
			})
			.UnwrapOr([]);

		Tech tech = new(vanilla, area, tier, categories, levels, dangerous, rare, requires, []);
		foreach (CWNode swapNode in node.Childs("technology_swap")) {
			if (OptionModule.IsNone(swapNode.Tag("name"))) {
				LogError(Messages.Data.TechUnnamedSwap.Format(node.Key));
				continue;
			}

			tech.Swaps[swapNode.Tag("name").Value.ToRawString()]
				= TechSwap.Parse(swapNode, cwComparer, tech);
		}

		return tech;
	}
}
