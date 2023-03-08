using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.FileHandlingServices;
using Oraide.LanguageServer.Caching;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class MapFileHandlingService : BaseFileHandlingService
	{
		public MapFileHandlingService(SymbolCache symbolCache, OpenFileCache openFileCache)
			: base(symbolCache, openFileCache) { }

		protected override IEnumerable<Location> KeyDefinition(CursorTarget cursorTarget)
		{
			// TODO: Not implemented yet.
			return Enumerable.Empty<Location>();
		}
	}
}
