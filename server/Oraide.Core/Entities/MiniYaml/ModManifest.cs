using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Oraide.Core.Entities.MiniYaml
{
	public readonly struct ModManifest
	{
		public readonly string MapFolder;

		public readonly IReadOnlyList<string> RulesFiles;

		public readonly IReadOnlyList<string> CursorsFiles;

		public readonly IReadOnlyList<string> WeaponsFiles;

		public ModManifest(IEnumerable<YamlNode> yamlNodes)
		{
			MapFolder = yamlNodes.FirstOrDefault(x => x.Key == "MapFolders")?.ChildNodes?.FirstOrDefault(x => x.Value == "System")?.Key;
			RulesFiles = new ReadOnlyCollection<string>(GetValues(yamlNodes, "Rules").ToList());
			CursorsFiles = new ReadOnlyCollection<string>(GetValues(yamlNodes, "Cursors").ToList());
			WeaponsFiles = new ReadOnlyCollection<string>(GetValues(yamlNodes, "Weapons").ToList());
		}

		static IEnumerable<string> GetValues(IEnumerable<YamlNode> yamlNodes, string key)
		{
			return yamlNodes.FirstOrDefault(x => x.Key == key)?.ChildNodes?.Select(x => x.Key) ?? Enumerable.Empty<string>();
		}
	}
}
