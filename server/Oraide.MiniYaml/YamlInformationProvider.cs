using System.Collections.Generic;
using System.IO;
using System.Linq;
using Oraide.Core.Entities.MiniYaml;
using Oraide.MiniYaml.Abstraction.SymbolGenerationStrategies;
using Oraide.MiniYaml.SymbolGenerationStrategies;

namespace Oraide.MiniYaml
{
	// The currently planned/supported use-cases for MiniYAML information are:
	//  - Generating symbols to navigate to.
	public class YamlInformationProvider
	{
		readonly string yamlFolder;
		readonly IMiniYamlSymbolGenerationStrategy symbolGenerator;

		public YamlInformationProvider(in string yamlFolder)
		{
			this.yamlFolder = yamlFolder;
			symbolGenerator = new MiniYamlSymbolGenerationStrategy(yamlFolder);
		}

		public IEnumerable<string> GetModDirectories()
		{
			return Directory.EnumerateFiles(yamlFolder, "mod.yaml", SearchOption.AllDirectories).Select(Path.GetDirectoryName);
		}

		public IEnumerable<YamlNode> ReadModFile(string modFolder)
		{
			return symbolGenerator.ParseModFile(modFolder);
		}

		public IEnumerable<YamlNode> ReadMapFile(string mapFolder)
		{
			return symbolGenerator.ParseMapFile(mapFolder);
		}

		public ILookup<string, ActorDefinition> GetActorDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods)
		{
			return symbolGenerator.GetActorDefinitions(referencedFiles, mods);
		}

		public ILookup<string, WeaponDefinition> GetWeaponDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods)
		{
			return symbolGenerator.GetWeaponDefinitions(referencedFiles, mods);
		}

		public ILookup<string, ConditionDefinition> GetConditionDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods)
		{
			return symbolGenerator.GetConditionDefinitions(referencedFiles, mods);
		}

		public ILookup<string, CursorDefinition> GetCursorDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods)
		{
			return symbolGenerator.GetCursorDefinitions(referencedFiles, mods);
		}

		public ILookup<string, PaletteDefinition> GetPaletteDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods, HashSet<string> knownPaletteTypes)
		{
			return symbolGenerator.GetPaletteDefinitions(referencedFiles, mods, knownPaletteTypes);
		}

		public ILookup<string, SpriteSequenceImageDefinition> GetSpriteSequenceDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods)
		{
			return symbolGenerator.GetSpriteSequenceDefinitions(referencedFiles, mods);
		}

		public (IEnumerable<YamlNode> Original, IEnumerable<YamlNode> Flattened) ParseText(string text, string fileUriString = null)
		{
			return symbolGenerator.ParseText(text, fileUriString);
		}
	}
}
