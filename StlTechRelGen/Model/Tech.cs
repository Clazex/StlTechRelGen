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

	// Only 3 of 11 properties: area and categories are what swaps can
	// override. All other properties never differ per swap.
	public TechSwap AsSwap() => new(Vanilla, Area, Categories);

	// Parse a value that may be either a plain integer or a @-prefixed
	// scripted variable reference. The @ prefix denotes a lookup into
	// common/scripted_variables/. Anything that is neither a plain
	// integer nor a @-prefixed variable reference is illegal and throws.
	private static int ParseIntOrVariable(
		string fieldValue,
		string fieldName,
		IReadOnlyDictionary<string, CWValue> variables
	) {
		if (int.TryParse(fieldValue, out int result)) {
			return result;
		}

		if (!fieldValue.StartsWith('@')) {
			throw new NotSupportedException(
				$"Unexpected {fieldName} value: {fieldValue}"
			);
		}

		return ((CWValue.Int) variables[fieldValue]).Item;
	}

	public static Tech Parse(
		CWNode node,
		CWComparer cwComparer,
		IReadOnlyDictionary<string, CWValue> variables
	) {
		bool vanilla = cwComparer.IsVanilla(node.Position);
		TechArea area = Enum.Parse<TechArea>(
			node.Tag("area").Value.ToRawString(), true
		);

		int tier = ParseIntOrVariable(
			node.Tag("tier").Value.ToRawString(), "tier", variables
		);

		List<string> categories = [
			.. node.Child("category").Value.LeafValues
				.Select(i => i.ValueText)
		];

		int levels = node.Tag("levels")
			.Map(x => ParseIntOrVariable(x.ToRawString(), "levels", variables))
			.UnwrapOr(1);

		// Stellaris represents booleans as the case-insensitive strings
		// "yes"/"no" rather than JSON-style true/false.
		bool dangerous = node.Tag("is_dangerous")
			.Map(x => x.ToRawString().Equals("yes", StringComparison.OrdinalIgnoreCase))
			.UnwrapOr(false);
		bool rare = node.Tag("is_rare")
			.Map(x => x.ToRawString().Equals("yes", StringComparison.OrdinalIgnoreCase))
			.UnwrapOr(false);

		// Prerequisites use a dual-track structure:
		//   LeafValues are direct tech IDs — all are required (implicit AND).
		//   Children named "OR" provide a set of tech IDs where any one
		//   of them satisfies the clause (explicit OR).
		// The two tracks are combined into a flat list of TechRequirement
		// instances, carrying the original AND/OR semantics downstream.
		List<TechRequirement> requires = node.Child("prerequisites")
			.Map(x => {
				List<TechRequirement> list = [
					.. x.LeafValues.Select(
						x => new TechRequirementSingle(x.Value.ToRawString())
					),
				];

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

		Tech tech = new(
			vanilla, area, tier, categories,
			levels, dangerous, rare, requires, []
		);
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
