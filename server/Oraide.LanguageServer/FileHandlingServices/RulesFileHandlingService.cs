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
	public partial class RulesFileHandlingService : BaseFileHandlingService
	{
		IEnumerable<CompletionItem> traitNames;
		IEnumerable<CompletionItem> actorNames;
		IEnumerable<CompletionItem> weaponNames;
		IEnumerable<CompletionItem> conditionNames;
		IEnumerable<CompletionItem> cursorNames;
		IEnumerable<CompletionItem> paletteNames;
		IEnumerable<CompletionItem> spriteSequenceImageNames;

		public RulesFileHandlingService(SymbolCache symbolCache, OpenFileCache openFileCache)
			: base(symbolCache, openFileCache) { }

		public override void Initialize(CursorTarget cursorTarget)
		{
			base.Initialize(cursorTarget);

			// TODO: Don't map everything to CompletionItems here! Defer that until we know what we need, then only map that (like in DefinitionHandler).
			// Using .First() is not great but we have no way to differentiate between traits of the same name
			// until the server learns the concept of a mod and loaded assemblies.
			traitNames = codeSymbols.TraitInfos.Where(x => !x.First().IsAbstract).Select(x => x.First().ToCompletionItem());
			actorNames = modSymbols.ActorDefinitions.Select(x => x.First().ToCompletionItem());
			weaponNames = modSymbols.WeaponDefinitions.Select(x => x.First().ToCompletionItem());
			conditionNames = modSymbols.ConditionDefinitions.Select(x => x.First().ToCompletionItem());
			cursorNames = modSymbols.CursorDefinitions.Select(x => x.First().ToCompletionItem());
			paletteNames = modSymbols.PaletteDefinitions.Select(x => x.First().ToCompletionItem());
			spriteSequenceImageNames = modSymbols.SpriteSequenceImageDefinitions.Select(x => x.First().ToCompletionItem());
		}

		#region Handler method implementations

		protected override IEnumerable<Location> ValueReferences(CursorTarget cursorTarget)
		{
			// TODO: Not implemented yet.
			return Enumerable.Empty<Location>();
		}

		#endregion

		#region Helper methods

		protected string ResolveSpriteSequenceImageNameForRules(CursorTarget cursorTarget, ClassFieldInfo fieldInfo, MapManifest? mapManifest)
		{
			var files = new List<string>(symbolCache[cursorTarget.ModId].ModManifest.RulesFiles);
			if (mapManifest?.RulesFiles != null)
				files.AddRange(mapManifest?.RulesFiles);

			return ResolveSpriteSequenceImageName(cursorTarget, fieldInfo, files);
		}

		#endregion
	}
}
