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
			var filePaths = Directory.EnumerateFiles(yamlFolder, "mod.yaml", SearchOption.AllDirectories).ToList();
			if (!filePaths.Any())
				filePaths.Add(yamlFolder);

			return filePaths.Select(Path.GetDirectoryName);
		}

		public IReadOnlyDictionary<string, ILookup<string, ActorDefinition>> GetActorDefinitions()
		{
			return OpenRAMiniYamlParser.GetActorDefinitions(yamlFolder);
		}

		public IReadOnlyDictionary<string, ILookup<string, WeaponDefinition>> GetWeaponDefinitions()
		{
			return OpenRAMiniYamlParser.GetWeaponDefinitions(yamlFolder);
		}

		public IReadOnlyDictionary<string, ILookup<string, ConditionDefinition>> GetConditionDefinitions()
		{
			return OpenRAMiniYamlParser.GetConditionDefinitions(yamlFolder);
		}

		public IReadOnlyDictionary<string, ILookup<string, CursorDefinition>> GetCursorDefinitions()
		{
			return OpenRAMiniYamlParser.GetCursorDefinitions(yamlFolder);
		}

		public (IEnumerable<YamlNode> Original, IEnumerable<YamlNode> Flattened) ParseText(string text)
		{
			return OpenRAMiniYamlParser.ParseText(text);
		}
	}
}
