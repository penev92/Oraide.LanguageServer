using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Extensions;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class SpriteSequencesFileHandlingService
	{
		protected override IEnumerable<CompletionItem> ValueCompletion(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetNodeIndentation switch
			{
				0 => Enumerable.Empty<CompletionItem>(),
				1 => HandleValueCompletionAt1(cursorTarget),
				2 => HandleValueCompletionAt2(cursorTarget),
				3 => Enumerable.Empty<CompletionItem>(),
				4 => HandleValueCompletionAt4(cursorTarget),
				_ => Enumerable.Empty<CompletionItem>()
			};
		}

		#region Private methods

		IEnumerable<CompletionItem> HandleValueCompletionAt1(CursorTarget cursorTarget)
		{
			if (cursorTarget.TargetNode.Key == "Inherits")
			{
				if (cursorTarget.FileType == FileType.MapSpriteSequences)
				{
					var mapReference = symbolCache[cursorTarget.ModId].Maps
						.FirstOrDefault(x => x.SpriteSequenceFiles.Contains(cursorTarget.FileReference));

					if (mapReference.MapReference != null && symbolCache.Maps.TryGetValue(mapReference.MapReference, out var mapSymbols))
						return spriteSequenceImageNames.Union(mapSymbols.SpriteSequenceImageDefinitions.Select(x => x.First().ToCompletionItem()));
				}

				return spriteSequenceImageNames;
			}

			return Enumerable.Empty<CompletionItem>();
		}

		IEnumerable<CompletionItem> HandleValueCompletionAt2(CursorTarget cursorTarget)
		{
			return HandleSpriteSequencePropertyValueCompletion(cursorTarget);
		}

		IEnumerable<CompletionItem> HandleValueCompletionAt4(CursorTarget cursorTarget)
		{
			if (cursorTarget.TargetNode.ParentNode.ParentNode.Key == "Combine")
				return HandleSpriteSequencePropertyValueCompletion(cursorTarget);

			return Enumerable.Empty<CompletionItem>();
		}

		IEnumerable<CompletionItem> HandleSpriteSequencePropertyValueCompletion(CursorTarget cursorTarget)
		{
			var spriteSequenceFormat = symbolCache[cursorTarget.ModId].ModManifest.SpriteSequenceFormat.Type;
			var spriteSequenceType = codeSymbols.SpriteSequenceInfos[spriteSequenceFormat].First();

			var fieldInfo = spriteSequenceType.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.Key);
			if (fieldInfo.Name != null)
			{
				// Try to check if this is an enum type field.
				var enumInfo = codeSymbols.EnumInfos
					.FirstOrDefault(x => x.Key == fieldInfo.InternalType);
				if (enumInfo != null)
				{
					return enumInfo.FirstOrDefault().Values.Select(x => new CompletionItem
					{
						Label = x,
						Kind = CompletionItemKind.EnumMember,
						Detail = "Enum type value",
						Documentation = $"{enumInfo.Key}.{x}"
					});
				}
			}

			if (fieldInfo.InternalType == "bool" || fieldInfo.InternalType == "Boolean")
			{
				return new[] { CompletionItems.True, CompletionItems.False };
			}

			return Enumerable.Empty<CompletionItem>();
		}

		#endregion
	}
}
