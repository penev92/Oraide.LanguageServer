using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Extensions;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class RulesFileHandlingService
	{
		protected override IEnumerable<CompletionItem> KeyCompletion(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetNodeIndentation switch
			{
				0 => HandleKeyCompletionAt0(cursorTarget),
				1 => HandleKeyCompletionAt1(cursorTarget),
				2 => HandleKeyCompletionAt2(cursorTarget),
				_ => Enumerable.Empty<CompletionItem>()
			};
		}

		#region Private methods

		IEnumerable<CompletionItem> HandleKeyCompletionAt0(CursorTarget cursorTarget)
		{
			// Get only actor definitions. Presumably for reference and for overriding purposes.
			return actorNames;
		}

		// TODO: Get only trait names that the actor actually has! (Will require inheritance resolution).
		IEnumerable<CompletionItem> HandleKeyCompletionAt1(CursorTarget cursorTarget)
		{
			// Get only traits (and "Inherits").
			return traitNames.Append(CompletionItems.Inherits);
		}

		// TODO: This will likely not handle trait property removals properly!
		IEnumerable<CompletionItem> HandleKeyCompletionAt2(CursorTarget cursorTarget)
		{
			// Get only trait properties.
			var traitName = cursorTarget.TargetNode.ParentNode.Key.Split('@')[0];
			var presentProperties = cursorTarget.TargetNode.ParentNode.ChildNodes.Select(x => x.Key).ToHashSet();

			// Getting all traits and then all their properties is not great but we have no way to differentiate between traits of the same name
			// until the server learns the concept of a mod and loaded assemblies.
			return codeSymbols.TraitInfos[traitName]
				.SelectMany(x => x.PropertyInfos)
				.DistinctBy(y => y.Name)
				.Where(x => !presentProperties.Contains(x.Name))
				.Select(z => z.ToCompletionItem());
		}

		#endregion
	}
}
