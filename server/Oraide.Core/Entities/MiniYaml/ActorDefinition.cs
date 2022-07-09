using System.Collections.Generic;

namespace Oraide.Core.Entities.MiniYaml
{
	public readonly struct ActorDefinition
	{
		public readonly string Name;

		public readonly string TooltipName;

		public readonly MemberLocation Location;

		public readonly List<ActorTraitDefinition> Traits; // This is redundant for the navigatable definitions list.

		public ActorDefinition(string name, string tooltipName, MemberLocation location, List<ActorTraitDefinition> traits)
		{
			Name = name;
			TooltipName = tooltipName;
			Location = location;
			Traits = traits;
		}

		public override string ToString() => $"{nameof(ActorDefinition)}: {Name} ({TooltipName}), {Traits.Count} Traits";
	}
}
