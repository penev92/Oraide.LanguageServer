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

		public MemberLocation Location { get; }

		public TraitDefinition(string name, MemberLocation location)
		{
			Name = name;
			Location = location;
		}
	}

	public readonly struct CursorTarget
	{
		public string ModId { get; }

		public YamlNode TargetNode { get; }

		// TODO: Change to enum.
		public string TargetType { get; }

		public string TargetString { get; }

		public MemberLocation TargetStart { get; }

		public MemberLocation TargetEnd { get; }

		public CursorTarget(string modId, YamlNode targetNode, string targetType, string targetString, MemberLocation targetStart, MemberLocation targetEnd)
		{
			ModId = modId;
			TargetNode = targetNode;
			TargetType = targetType;
			TargetString = targetString;
			TargetStart = targetStart;
			TargetEnd = targetEnd;
		}
	}
}
