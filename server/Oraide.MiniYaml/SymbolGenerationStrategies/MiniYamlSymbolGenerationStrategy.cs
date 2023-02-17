using System.Collections.Generic;
using System.Linq;
using Oraide.Core.Entities.MiniYaml;
using Oraide.MiniYaml.Abstraction.Parsers;
using Oraide.MiniYaml.Abstraction.SymbolGenerationStrategies;
using Oraide.MiniYaml.Parsers;

namespace Oraide.MiniYaml.SymbolGenerationStrategies
{
	class MiniYamlSymbolGenerationStrategy : IMiniYamlSymbolGenerationStrategy
	{
		readonly IMiniYamlParser selectedParser;

		public MiniYamlSymbolGenerationStrategy(string openRaFolder)
		{
			var parsers = new IMiniYamlParser[]
			{
				new BleedMiniYamlParser(),
				new Pre202301MiniYamlParser()
			};

			selectedParser = parsers.First(x => x.CanParse(openRaFolder));
		}

		public IEnumerable<YamlNode> ParseModFile(in string modFolder)
		{
			return selectedParser.ParseModFile(modFolder);
		}

		public IEnumerable<YamlNode> ParseMapFile(in string mapFolder)
		{
			return selectedParser.ParseMapFile(mapFolder);
		}

		public ILookup<string, ActorDefinition> GetActorDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods)
		{
			return selectedParser.ParseActorDefinitions(referencedFiles, mods).ToLookup(x => x.Name, y => y);
		}

		public ILookup<string, WeaponDefinition> GetWeaponDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods)
		{
			return selectedParser.ParseWeaponDefinitions(referencedFiles, mods).ToLookup(x => x.Name, y => y);
		}

		public ILookup<string, ConditionDefinition> GetConditionDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods)
		{
			return selectedParser.ParseConditionDefinitions(referencedFiles, mods).ToLookup(x => x.Name, y => y);
		}

		public ILookup<string, CursorDefinition> GetCursorDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods)
		{
			return selectedParser.ParseCursorDefinitions(referencedFiles, mods).ToLookup(x => x.Name, y => y);
		}

		public ILookup<string, PaletteDefinition> GetPaletteDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods, HashSet<string> knownPaletteTypes)
		{
			return selectedParser.ParsePaletteDefinitions(referencedFiles, mods, knownPaletteTypes).ToLookup(x => x.Name, y => y);
		}

		public ILookup<string, SpriteSequenceImageDefinition> GetSpriteSequenceDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods)
		{
			return selectedParser.ParseSpriteSequenceDefinitions(referencedFiles, mods).ToLookup(x => x.Name, y => y);
		}

		public (IEnumerable<YamlNode> Original, IEnumerable<YamlNode> Flattened) ParseText(string text, string fileUriString = null)
		{
			return selectedParser.ParseText(text, fileUriString);
		}
	}
}
