using System.Linq;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.FileHandlingServices;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class SpriteSequencesFileHandlingService
	{
		protected override Hover ValueHover(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetNodeIndentation switch
			{
				0 => null,
				1 => HandleValueHoverAt1(cursorTarget),
				2 => HandleValueHoverAt2(cursorTarget),
				3 => HandleValueHoverAt3(cursorTarget),
				4 => HandleValueHoverAt4(cursorTarget),
				_ => null
			};
		}

		#region Private methods

		Hover HandleValueHoverAt1(CursorTarget cursorTarget)
		{
			if (cursorTarget.TargetNode.Key == "Inherits")
			{
				if (modSymbols.SpriteSequenceImageDefinitions.Contains(cursorTarget.TargetString))
					return IHoverService.HoverFromHoverInfo($"```csharp\nImage \"{cursorTarget.TargetString}\"\n```", range);

				if (cursorTarget.FileType == FileType.MapSpriteSequences)
				{
					var mapReference = symbolCache[cursorTarget.ModId].Maps
						.FirstOrDefault(x => x.SpriteSequenceFiles.Contains(cursorTarget.FileReference));

					if (mapReference.MapReference != null && symbolCache.Maps.TryGetValue(mapReference.MapReference, out var mapSymbols))
						if (mapSymbols.SpriteSequenceImageDefinitions.Contains(cursorTarget.TargetString))
							return IHoverService.HoverFromHoverInfo($"```csharp\nImage \"{cursorTarget.TargetString}\"\n```", range);
				}
			}

			// This was only valid before the sequence definitions rework that was merged in January 2023.
			else if (symbolCache.YamlVersion == nameof(MiniYaml.Parsers.Pre202301MiniYamlParser))
			{
				return HandleSpriteSequenceFileName(cursorTarget);
			}

			return null;
		}

		Hover HandleValueHoverAt2(CursorTarget cursorTarget)
		{
			return HandleSpriteSequencePropertyValueHover(cursorTarget);
		}

		Hover HandleValueHoverAt3(CursorTarget cursorTarget)
		{
			if (cursorTarget.TargetNode.ParentNode.Key == "Combine")
				return HandleSpriteSequenceFileName(cursorTarget);

			return null;
		}

		Hover HandleValueHoverAt4(CursorTarget cursorTarget)
		{
			if (cursorTarget.TargetNode.ParentNode.ParentNode.Key == "Combine")
				return HandleSpriteSequencePropertyValueHover(cursorTarget);

			return null;
		}

		Hover HandleSpriteSequenceFileName(CursorTarget cursorTarget)
		{
			var content = $"```csharp\nFile \"{cursorTarget.TargetString}\"\n```";
			return IHoverService.HoverFromHoverInfo(content, range);
		}

		Hover HandleSpriteSequencePropertyValueHover(CursorTarget cursorTarget)
		{
			var spriteSequenceFormat = symbolCache[cursorTarget.ModId].ModManifest.SpriteSequenceFormat.Type;
			var spriteSequenceType = codeSymbols.SpriteSequenceInfos[spriteSequenceFormat].First();

			var fieldInfo = spriteSequenceType.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.Key);
			if (fieldInfo.Name != null)
			{
				// Try to check if this is an enum type field.
				var enumInfo = codeSymbols.EnumInfos.FirstOrDefault(x => x.Key == fieldInfo.InternalType);
				if (enumInfo != null)
				{
					var content = $"```csharp\n{enumInfo.Key}.{cursorTarget.TargetString}\n```";
					return IHoverService.HoverFromHoverInfo(content, range);
				}
			}

			return null;
		}

		#endregion
	}
}
