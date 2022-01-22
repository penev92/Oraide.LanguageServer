using System;

namespace Oraide.Core.Entities.MiniYaml
{
	[Flags]
	public enum CursorTargetType
	{
		Unknown = 0,
		Indent1 = 1,
		Indent2 = 2,
		Indent3 = 4,
		Indent4 = 8,
		Indent5 = 16,
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

		public int TargetNodeIndentation { get; }

		public CursorTarget(string modId, YamlNode targetNode, string targetType, string targetString,
			MemberLocation targetStart, MemberLocation targetEnd, int targetNodeIndentation)
		{
			ModId = modId;
			TargetNode = targetNode;
			TargetType = targetType;
			TargetString = targetString;
			TargetStart = targetStart;
			TargetEnd = targetEnd;
			TargetNodeIndentation = targetNodeIndentation;
		}
	}
}
