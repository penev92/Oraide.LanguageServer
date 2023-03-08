using System.Collections.Generic;
using Oraide.Core.Entities.Csharp;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.FileHandlingServices;
using Oraide.LanguageServer.Caching;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class SpriteSequencesFileHandlingService : BaseFileHandlingService
	{
		public SpriteSequencesFileHandlingService(SymbolCache symbolCache, OpenFileCache openFileCache)
			: base(symbolCache, openFileCache) { }
	}
}
