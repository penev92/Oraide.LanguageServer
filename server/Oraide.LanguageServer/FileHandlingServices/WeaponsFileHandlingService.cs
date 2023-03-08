using System.Collections.Generic;
using Oraide.Core.Entities.Csharp;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.FileHandlingServices;
using Oraide.LanguageServer.Caching;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class WeaponsFileHandlingService : BaseFileHandlingService
	{
		WeaponInfo weaponInfo;

		public WeaponsFileHandlingService(SymbolCache symbolCache, OpenFileCache openFileCache)
			: base(symbolCache, openFileCache) { }

		protected override void Initialize(CursorTarget cursorTarget)
		{
			base.Initialize(cursorTarget);
			weaponInfo = symbolCache[cursorTarget.ModId].CodeSymbols.WeaponInfo;
		}

		protected string ResolveSpriteSequenceImageNameForWeapons(CursorTarget cursorTarget, ClassFieldInfo fieldInfo, MapManifest? mapManifest)
		{
			var files = new List<string>(symbolCache[cursorTarget.ModId].ModManifest.WeaponsFiles);
			if (mapManifest?.WeaponsFiles != null)
				files.AddRange(mapManifest?.WeaponsFiles);

			return ResolveSpriteSequenceImageName(cursorTarget, fieldInfo, files);
		}
	}
}
