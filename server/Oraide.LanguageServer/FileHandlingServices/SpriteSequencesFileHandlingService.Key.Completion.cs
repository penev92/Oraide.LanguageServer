using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Extensions;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class SpriteSequencesFileHandlingService
	{
		protected override IEnumerable<CompletionItem> KeyCompletion(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetNodeIndentation switch
			{
				0 => HandleKeyCompletionAt0(cursorTarget),
				1 => HandleKeyCompletionAt1(cursorTarget),
				2 => HandleKeyCompletionAt2(cursorTarget),
				3 => Enumerable.Empty<CompletionItem>(),
				4 => HandleKeyCompletionAt4(cursorTarget),
				_ => Enumerable.Empty<CompletionItem>()
			};
		}

		#region Private methods

		IEnumerable<CompletionItem> HandleKeyCompletionAt0(CursorTarget cursorTarget)
		{
			// Get only sprite sequence image definitions. Presumably for reference and for overriding purposes.
			return spriteSequenceImageNames;
		}

		IEnumerable<CompletionItem> HandleKeyCompletionAt1(CursorTarget cursorTarget)
		{
			// Get only "Inherits" and "Defaults".
			return new[] { CompletionItems.Inherits, CompletionItems.Defaults };
		}

		IEnumerable<CompletionItem> HandleKeyCompletionAt2(CursorTarget cursorTarget)
		{
			return HandleSpriteSequencePropertyKeyCompletion(cursorTarget);
		}

		IEnumerable<CompletionItem> HandleKeyCompletionAt4(CursorTarget cursorTarget)
		{
			if (cursorTarget.TargetNode.ParentNode.ParentNode.Key == "Combine")
				return HandleSpriteSequencePropertyKeyCompletion(cursorTarget);

			return Enumerable.Empty<CompletionItem>();
		}

		IEnumerable<CompletionItem> HandleSpriteSequencePropertyKeyCompletion(CursorTarget cursorTarget)
		{
			var presentProperties = cursorTarget.TargetNode.ParentNode.ChildNodes.Select(x => x.Key).ToHashSet();
			var spriteSequenceFormat = symbolCache[cursorTarget.ModId].ModManifest.SpriteSequenceFormat.Type;

			return codeSymbols.SpriteSequenceInfos[spriteSequenceFormat]
				.SelectMany(x => x.PropertyInfos)
				.DistinctBy(y => y.Name)
				.Where(x => !presentProperties.Contains(x.Name))
				.Select(z => z.ToCompletionItem());
		}

		#endregion
	}
}
