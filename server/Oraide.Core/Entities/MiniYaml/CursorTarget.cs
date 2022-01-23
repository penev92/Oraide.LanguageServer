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

	public enum FileType
	{
		Unknown = 0,
		Rules = 1,
		Weapons = 2
	}

	public readonly struct CursorTarget
	{
		public string ModId { get; }

		public FileType FileType { get; }

		public YamlNode TargetNode { get; }

		// TODO: Change to enum.
		public string TargetType { get; }

		public string TargetString { get; }

		public MemberLocation TargetStart { get; }

		public MemberLocation TargetEnd { get; }

		public int TargetNodeIndentation { get; }

		public CursorTarget(string modId, FileType fileType, YamlNode targetNode, string targetType, string targetString,
			MemberLocation targetStart, MemberLocation targetEnd, int targetNodeIndentation)
		{
			ModId = modId;
			FileType = fileType;
			TargetNode = targetNode;
			TargetType = targetType;
			TargetString = targetString;
			TargetStart = targetStart;
			TargetEnd = targetEnd;
			TargetNodeIndentation = targetNodeIndentation;
		}
	}
}
