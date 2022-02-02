using System.Collections.Generic;

namespace Oraide.Core.Entities.MiniYaml
{
	public readonly struct ActorDefinition
	{
		public readonly string Name;

		public readonly MemberLocation Location;

		public readonly List<ActorTraitDefinition> Traits; // This is redundant for the navigatable definitions list.

		public ActorDefinition(string name, MemberLocation location, List<ActorTraitDefinition> traits)
		{
			Name = name;
			Location = location;
			Traits = traits;
		}
	}
}
