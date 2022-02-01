using System;
using LspTypes;
using Oraide.Core.Entities;
using Oraide.Core.Entities.MiniYaml;
using Range = LspTypes.Range;

namespace Oraide.LanguageServer.Extensions
{
	static class LspTypesExtensions
	{
		public static Range ToRange(this CursorTarget cursorTarget)
		{
			return new Range
			{
				Start = new Position((uint)cursorTarget.TargetStart.LineNumber, (uint)cursorTarget.TargetStart.CharacterPosition),
				End = new Position((uint)cursorTarget.TargetEnd.LineNumber, (uint)cursorTarget.TargetEnd.CharacterPosition)
			};
		}

		public static Range ToRange(this MemberLocation memberLocation, int length)
		{
			return new Range
			{
				Start = new Position((uint)memberLocation.LineNumber - 1, (uint)memberLocation.CharacterPosition),
				End = new Position((uint)memberLocation.LineNumber - 1, (uint)(memberLocation.CharacterPosition + (uint)length))
			};
		}

		public static Location ToLspLocation(this MemberLocation memberLocation, int length)
		{
			return new Location
			{
				Uri = new Uri(memberLocation.FilePath).ToString(),
				Range = memberLocation.ToRange(length)
			};
		}
	}
}
