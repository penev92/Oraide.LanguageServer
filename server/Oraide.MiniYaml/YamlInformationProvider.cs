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

		public ILookup<string, ActorDefinition> GetActorDefinitions(string modFolder)
		{
			return OpenRAMiniYamlParser.GetActorDefinitions(modFolder);
		}

		public ILookup<string, WeaponDefinition> GetWeaponDefinitions(string modFolder)
		{
			return OpenRAMiniYamlParser.GetWeaponDefinitions(modFolder);
		}

		public ILookup<string, ConditionDefinition> GetConditionDefinitions(string modFolder)
		{
			return OpenRAMiniYamlParser.GetConditionDefinitions(modFolder);
		}

		public ILookup<string, CursorDefinition> GetCursorDefinitions(string modFolder)
		{
			return OpenRAMiniYamlParser.GetCursorDefinitions(modFolder);
		}

		public (IEnumerable<YamlNode> Original, IEnumerable<YamlNode> Flattened) ParseText(string text)
		{
			return OpenRAMiniYamlParser.ParseText(text);
		}
	}
}
