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

		public ILookup<string, ActorDefinition> GetActorDefinitions()
		{
			return OpenRAMiniYamlParser.GetActorDefinitions(yamlFolder);
		}
	}
}
