using System.Collections.Generic;

namespace Oraide.Core.Entities.MiniYaml
{
	public struct ActorDefinition
	{
		public string Name { get; set; }

		public MemberLocation Location { get; }

		public List<TraitDefinition> Traits { get; } // This is redundant for the navigatable definitions list.

		public ActorDefinition(string name, MemberLocation location, List<TraitDefinition> traits)
		{
			Name = name;
			Location = location;
			Traits = traits;
		}
	}

	public readonly struct TraitDefinition
	{
		public string Name { get; }

		public string FullName { get; }

		public MemberLocation Location { get; }

		public TraitDefinition(string name, MemberLocation location)
		{
			FullName = name;
			Name = name.Split('@')[0];
			Location = location;
		}
	}
}
