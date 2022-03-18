using System.Collections.Generic;
using System.IO;
using System.Linq;
using Oraide.Core.Entities.MiniYaml;
using Oraide.MiniYaml.YamlParsers;

namespace Oraide.MiniYaml
{
	// The currently planned/supported use-cases for MiniYAML information are:
	//  - Generating symbols to navigate to.
	public class YamlInformationProvider
	{
		readonly string yamlFolder;

		public YamlInformationProvider(string yamlFolder)
		{
			this.yamlFolder = yamlFolder;
		}

		public IEnumerable<string> GetModDirectories()
		{
			return Directory.EnumerateFiles(yamlFolder, "mod.yaml", SearchOption.AllDirectories).Select(Path.GetDirectoryName);
		}

		public IEnumerable<YamlNode> ReadModFile(string modFolder)
		{
			return OpenRAMiniYamlParser.ReadModFile(modFolder);
		}

		public IEnumerable<YamlNode> ReadMapFile(string mapFolder)
		{
			return OpenRAMiniYamlParser.ReadMapFile(mapFolder);
		}

		public ILookup<string, ActorDefinition> GetActorDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods)
		{
			return OpenRAMiniYamlParser.GetActorDefinitions(referencedFiles, mods);
		}

		public ILookup<string, WeaponDefinition> GetWeaponDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods)
		{
			return OpenRAMiniYamlParser.GetWeaponDefinitions(referencedFiles, mods);
		}

		public ILookup<string, ConditionDefinition> GetConditionDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods)
		{
			return OpenRAMiniYamlParser.GetConditionDefinitions(referencedFiles, mods);
		}

		public ILookup<string, CursorDefinition> GetCursorDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods)
		{
			return OpenRAMiniYamlParser.GetCursorDefinitions(referencedFiles, mods);
		}

		public (IEnumerable<YamlNode> Original, IEnumerable<YamlNode> Flattened) ParseText(string text)
		{
			return OpenRAMiniYamlParser.ParseText(text);
		}
	}
}
