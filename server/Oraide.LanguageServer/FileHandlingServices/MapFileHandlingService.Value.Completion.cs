using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Extensions;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class MapFileHandlingService
	{
		protected override IEnumerable<CompletionItem> ValueCompletion(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetNodeIndentation switch
			{
				0 => Enumerable.Empty<CompletionItem>(),
				1 => HandleValueCompletionAt1(cursorTarget),
				_ => Enumerable.Empty<CompletionItem>()
			};
		}

		#region Private methods

		IEnumerable<CompletionItem> HandleValueCompletionAt1(CursorTarget cursorTarget)
		{
			if (cursorTarget.TargetNode?.ParentNode?.Key == "Actors")
			{
				// Actor definitions from map rules:
				var mapReference = symbolCache[cursorTarget.ModId].Maps
					.FirstOrDefault(x => x.MapFileReference == cursorTarget.FileReference);

				if (mapReference.MapReference != null && symbolCache.Maps.TryGetValue(mapReference.MapReference, out var mapSymbols))
					return actorNames.Union(mapSymbols.ActorDefinitions.Select(x => x.First().ToCompletionItem()));

				return actorNames;
			}

			return Enumerable.Empty<CompletionItem>();
		}

		#endregion
	}
}
