using System.Collections.Generic;

namespace Oraide.Core.Entities.MiniYaml
{
	public struct ActorDefinition
	{
		public string Name { get; set; }

		public MemberLocation Location { get; }

		public List<ActorTraitDefinition> Traits { get; } // This is redundant for the navigatable definitions list.

		public ActorDefinition(string name, MemberLocation location, List<ActorTraitDefinition> traits)
		{
			Name = name;
			Location = location;
			Traits = traits;
		}
	}
}
