using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.FileHandlingServices;
using Oraide.LanguageServer.Caching;
using Oraide.LanguageServer.Extensions;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class MapFileHandlingService : BaseFileHandlingService
	{
		IEnumerable<CompletionItem> actorNames;

		public MapFileHandlingService(SymbolCache symbolCache, OpenFileCache openFileCache)
			: base(symbolCache, openFileCache) { }

		protected override void Initialize(CursorTarget cursorTarget)
		{
			base.Initialize(cursorTarget);

			// TODO: Don't map everything to CompletionItems here! Defer that until we know what we need, then only map that (like in DefinitionHandler).
			// Using .First() is not great but we have no way to differentiate between traits of the same name
			// until the server learns the concept of a mod and loaded assemblies.
			actorNames = modSymbols.ActorDefinitions.Select(x => x.First().ToCompletionItem());
		}

		protected override IEnumerable<Location> KeyDefinition(CursorTarget cursorTarget)
		{
			// TODO: Not implemented yet.
			return Enumerable.Empty<Location>();
		}

		protected override IEnumerable<CompletionItem> KeyCompletion(CursorTarget cursorTarget)
		{
			// TODO: Not implemented yet.
			return Enumerable.Empty<CompletionItem>();
		}
	}
}
