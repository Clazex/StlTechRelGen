namespace StlTechRelGen.Model;

internal sealed class TechTable {
	public ReadOnlyDictionary<string, Tech> Techs { get; private init; }

	public ReadOnlySet<string> AllTechIds { get; }

	public TechTable(ProgressContext ctx, Dictionary<string, Tech> dict) {
		ProgressTask task = ctx.AddTask(Messages.Progress.BuildingTechRelationship).MaxValue(dict.Count);
		foreach ((string id, Tech tech) in dict) {
			foreach (TechRequirement requirement in tech.Requires) {
				foreach (string reqId in requirement.Requirements()) {
					if (dict.TryGetValue(reqId, out Tech? reqTech)) {
						reqTech.Unlocks.Add(id);
					} else {
						LogWarning(Messages.Data.TechUnknownPrereq.Format(id, reqId));
					}
				}
			}

			task.Increment(1);
		}

		task.StopTask();

		Techs = dict.AsReadOnly();
		AllTechIds = Techs.SelectMany(i => i.Value.Swaps.Keys.Prepend(i.Key)).ToHashSet().AsReadOnly();
	}
}
