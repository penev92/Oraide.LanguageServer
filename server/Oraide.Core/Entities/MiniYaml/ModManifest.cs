using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Oraide.Core.Entities.MiniYaml
{
	public readonly struct ModManifest
	{
		public readonly string MapsFolder;

		public readonly IReadOnlyList<string> RulesFiles;

		public readonly IReadOnlyList<string> CursorsFiles;

		public readonly IReadOnlyList<string> WeaponsFiles;

		public readonly IReadOnlyList<string> SpriteSequences;

		public readonly (string Type, IReadOnlyDictionary<string, YamlNode> Metadata) SpriteSequenceFormat;

		public ModManifest(IEnumerable<YamlNode> yamlNodes)
		{
			MapsFolder = yamlNodes.FirstOrDefault(x => x.Key == "MapFolders")?.ChildNodes?.FirstOrDefault(x => x.Value == "System")?.Key;
			RulesFiles = new ReadOnlyCollection<string>(GetValues(yamlNodes, "Rules").ToList());
			CursorsFiles = new ReadOnlyCollection<string>(GetValues(yamlNodes, "Cursors").ToList());
			WeaponsFiles = new ReadOnlyCollection<string>(GetValues(yamlNodes, "Weapons").ToList());
			SpriteSequences = new ReadOnlyCollection<string>(GetValues(yamlNodes, "Sequences").ToList());

			var spriteSequenceFormatNode = yamlNodes.FirstOrDefault(x => x.Key == "SpriteSequenceFormat");
			SpriteSequenceFormat = (spriteSequenceFormatNode?.Value, spriteSequenceFormatNode?.ChildNodes?.ToDictionary(x => x.Key, y => y));
		}

		static IEnumerable<string> GetValues(IEnumerable<YamlNode> yamlNodes, string key)
		{
			return yamlNodes.FirstOrDefault(x => x.Key == key)?.ChildNodes?.Select(x => x.Key) ?? Enumerable.Empty<string>();
		}
	}
}
