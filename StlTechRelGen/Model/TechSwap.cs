using StlTechRelGen.Utils;

using CWNode = CWTools.Process.Node;

namespace StlTechRelGen.Model;

internal sealed class TechSwap(
	bool vanilla,
	TechArea area,
	List<string> categories
) {
	public bool Vanilla { get; private init; } = vanilla;
	public TechArea Area { get; private init; } = area;
	public List<string> Categories { get; private init; } = categories;

	public static TechSwap Parse(CWNode node, CWComparer cwComparer, Tech parent) => new(
		cwComparer.IsVanilla(node.Position),
		node.Tag("area").Map(x => Enum.Parse<TechArea>(x.ToRawString(), true)).UnwrapOr(parent.Area),
		node.Child("category").Map(x => x.LeafValues.Select(i => i.ValueText).ToList()).UnwrapOr(parent.Categories)
	);
}
