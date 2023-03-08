using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.Core;
using Oraide.Core.Entities.Csharp;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.FileHandlingServices;
using Oraide.LanguageServer.Caching;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class SpriteSequencesFileHandlingService
	{
		protected override Hover KeyHover(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetNodeIndentation switch
			{
				0 => HandleKeyHoverAt0(cursorTarget),
				1 => HandleKeyHoverAt1(cursorTarget),
				2 => HandleKeyHoverAt2(cursorTarget),
				3 => HandleKeyHoverAt3(cursorTarget),
				4 => HandleKeyHoverAt4(cursorTarget),
				_ => null
			};
		}

		#region Private methods

		Hover HandleKeyHoverAt0(CursorTarget cursorTarget)
		{
			var content = $"```csharp\nImage \"{cursorTarget.TargetString}\"\n```";
			return IHoverService.HoverFromHoverInfo(content, range);
		}

		Hover HandleKeyHoverAt1(CursorTarget cursorTarget)
		{
			if (cursorTarget.TargetString == "Inherits")
				return IHoverService.HoverFromHoverInfo($"Inherits (and possibly overwrites) sequences from image {cursorTarget.TargetNode.Value}", range);

			if (cursorTarget.TargetString == "Defaults")
				return IHoverService.HoverFromHoverInfo("Sets default values for all sequences of this image.", range);

			return HandleSpriteSequenceName(cursorTarget);
		}

		Hover HandleKeyHoverAt2(CursorTarget cursorTarget)
		{
			return HandleSpriteSequencePropertyKeyHover(cursorTarget);
		}

		Hover HandleKeyHoverAt3(CursorTarget cursorTarget)
		{
			if (cursorTarget.TargetNode.ParentNode.Key == "Combine")
				return HandleSpriteSequenceName(cursorTarget);

			return null;
		}

		Hover HandleKeyHoverAt4(CursorTarget cursorTarget)
		{
			if (cursorTarget.TargetNode.ParentNode.ParentNode.Key == "Combine")
				return HandleSpriteSequencePropertyKeyHover(cursorTarget);

			return null;
		}

		Hover HandleSpriteSequenceName(CursorTarget cursorTarget)
		{
			var content = $"```csharp\nSequence \"{cursorTarget.TargetString}\"\n```";
			return IHoverService.HoverFromHoverInfo(content, range);
		}

		Hover HandleSpriteSequencePropertyKeyHover(CursorTarget cursorTarget)
		{
			var spriteSequenceFormat = symbolCache[cursorTarget.ModId].ModManifest.SpriteSequenceFormat.Type;
			var fieldInfo = codeSymbols.SpriteSequenceInfos[spriteSequenceFormat].FirstOrDefault().PropertyInfos
				.FirstOrDefault(x => x.Name == cursorTarget.TargetString);

			var content = fieldInfo.ToMarkdownInfoString();
			return IHoverService.HoverFromHoverInfo(content, range);
		}

		#endregion
	}
}
