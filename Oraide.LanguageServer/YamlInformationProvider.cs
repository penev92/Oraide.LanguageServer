using System.Linq;
using Oraide.LanguageServer.YamlParsers;

namespace Oraide.LanguageServer
{
	// The currently planned/supported use-cases for MiniYAML information are:
	//  - Generating symbols to navigate to.
	class YamlInformationProvider
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
