﻿using System;

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
		Unknown = -1,
		ModFile = 0,
		Rules = 1,
		Weapons = 2,
		SpriteSequences = 3,
		Cursors = 4,
		ChromeLayout = 5,
		MapFile = 10,
		MapRules = 11,
		MapWeapons = 12,
		MapSpriteSequences = 13,
	}

	public readonly struct CursorTarget
	{
		public readonly string ModId;

		public readonly FileType FileType;

		public readonly string FileReference;

		public readonly YamlNode TargetNode;

		// TODO: Change to enum.
		public readonly string TargetType;

		public readonly string TargetString;

		public readonly MemberLocation TargetStart;

		public readonly MemberLocation TargetEnd;

		public readonly int TargetNodeIndentation;

		public CursorTarget(string modId, FileType fileType, string fileReference, YamlNode targetNode, string targetType, string targetString,
			MemberLocation targetStart, MemberLocation targetEnd, int targetNodeIndentation)
		{
			ModId = modId;
			FileType = fileType;
			FileReference = fileReference;
			TargetNode = targetNode;
			TargetType = targetType;
			TargetString = targetString;
			TargetStart = targetStart;
			TargetEnd = targetEnd;
			TargetNodeIndentation = targetNodeIndentation;
		}
	}
}
