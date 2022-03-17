using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Oraide.Core.Entities.MiniYaml
{
	public readonly struct MapManifest
	{
		public readonly string MapFolder;

		public readonly IReadOnlyList<string> RulesFiles;

		public readonly IReadOnlyList<string> WeaponsFiles;

		public string MapFile => Path.Combine(MapFolder, "map.yaml");

		public MapManifest(string mapFolder, IEnumerable<YamlNode> yamlNodes, string mapsFolder)
		{
			MapFolder = mapFolder;
			RulesFiles = new ReadOnlyCollection<string>(GetValues(yamlNodes, "Rules", Path.GetFileName(mapFolder), mapsFolder).ToList());
			WeaponsFiles = new ReadOnlyCollection<string>(GetValues(yamlNodes, "Weapons", Path.GetFileName(mapFolder), mapsFolder).ToList());
		}

		static IEnumerable<string> GetValues(IEnumerable<YamlNode> yamlNodes, string key, string mapFolder, string mapsFolder)
		{
			var files = yamlNodes.FirstOrDefault(x => x.Key == key)?.Value?.Split(',').Select(x => x.Trim());
			return files?.Select(x => x.Contains('|') ? x : $"{mapsFolder}/{mapFolder}/{x}") ?? Enumerable.Empty<string>();
		}
	}
}
