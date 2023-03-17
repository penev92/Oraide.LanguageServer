using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.Core.Entities.Csharp;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.FileHandlingServices;
using Oraide.LanguageServer.Caching;
using Oraide.LanguageServer.Extensions;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class WeaponsFileHandlingService : BaseFileHandlingService
	{
		IEnumerable<CompletionItem> weaponNames;
		IEnumerable<CompletionItem> paletteNames;
		IEnumerable<CompletionItem> spriteSequenceImageNames;

		public WeaponsFileHandlingService(SymbolCache symbolCache, OpenFileCache openFileCache)
			: base(symbolCache, openFileCache) { }

		public override void Initialize(CursorTarget cursorTarget)
		{
			base.Initialize(cursorTarget);

			// TODO: Don't map everything to CompletionItems here! Defer that until we know what we need, then only map that (like in DefinitionHandler).
			// Using .First() is not great but we have no way to differentiate between traits of the same name
			// until the server learns the concept of a mod and loaded assemblies.
			weaponNames = modSymbols.WeaponDefinitions.Select(x => x.First().ToCompletionItem());
			paletteNames = modSymbols.PaletteDefinitions.Select(x => x.First().ToCompletionItem());
			spriteSequenceImageNames = modSymbols.SpriteSequenceImageDefinitions.Select(x => x.First().ToCompletionItem());
		}

		#region Handler method implementations

		// TODO: Not implemented yet.
		protected override IEnumerable<Location> KeyReferences(CursorTarget cursorTarget) => Enumerable.Empty<Location>();

		#endregion

		#region Helper methods

		protected string ResolveSpriteSequenceImageNameForWeapons(CursorTarget cursorTarget, ClassFieldInfo fieldInfo, MapManifest? mapManifest)
		{
			var files = new List<string>(symbolCache[cursorTarget.ModId].ModManifest.WeaponsFiles);
			if (mapManifest?.WeaponsFiles != null)
				files.AddRange(mapManifest?.WeaponsFiles);

			return ResolveSpriteSequenceImageName(cursorTarget, fieldInfo, files);
		}

		#endregion
	}
}
