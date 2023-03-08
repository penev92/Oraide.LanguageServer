using System.Collections.Generic;
using Oraide.Core.Entities.Csharp;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.FileHandlingServices;
using Oraide.LanguageServer.Caching;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class RulesFileHandlingService : BaseFileHandlingService
	{

		public RulesFileHandlingService(SymbolCache symbolCache, OpenFileCache openFileCache)
			: base(symbolCache, openFileCache) { }

		protected string ResolveSpriteSequenceImageNameForRules(CursorTarget cursorTarget, ClassFieldInfo fieldInfo, MapManifest? mapManifest)
		{
			var files = new List<string>(symbolCache[cursorTarget.ModId].ModManifest.RulesFiles);
			if (mapManifest?.RulesFiles != null)
				files.AddRange(mapManifest?.RulesFiles);

			return ResolveSpriteSequenceImageName(cursorTarget, fieldInfo, files);
		}
	}
}
