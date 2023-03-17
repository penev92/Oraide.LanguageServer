using System.Collections.Generic;
using System.IO;
using System.Linq;
using Oraide.Core.Entities;
using Oraide.Core.Entities.MiniYaml;
using Oraide.MiniYaml.Abstraction.Parsers;

namespace Oraide.MiniYaml.Parsers
{
	public class BleedMiniYamlParser : BaseMiniYamlParser
	{
		public override bool CanParse(in string folderPath)
		{
			return Directory.EnumerateFiles(folderPath, "*.yaml", SearchOption.AllDirectories).Any(x => File.ReadAllText(x).Contains("\tDefaults:\n\t\tFilename: "));
		}

		// File names are no longer the sequence node's value, but rather their own node.
		// Since we're keeping the FileName property on SpriteSequenceImageDefinition for backward compatibility, we need to fill it somehow.
		public override IEnumerable<SpriteSequenceImageDefinition> ParseSpriteSequenceDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods)
		{
			var yamlNodes = YamlNodesFromReferencedFiles(referencedFiles, mods);
			foreach (var node in yamlNodes)
			{
				var location = new MemberLocation(node.Location.FileUri, node.Location.LineNumber, node.Location.CharacterPosition);

				var defaults = node.ChildNodes?.FirstOrDefault(x => x.Key == "Defaults");
				var defaultFilename = defaults?.ChildNodes?.FirstOrDefault(x => x.Key == "Filename")?.Value;

				var sequences = node.ChildNodes == null
					? Enumerable.Empty<SpriteSequenceDefinition>()
					: node.ChildNodes.Select(x =>
					{
						var filename = x.ChildNodes?.FirstOrDefault(x => x.Key == "Filename")?.Value;
						var loc = new MemberLocation(x.Location.FileUri, x.Location.LineNumber, 1); // HACK HACK HACK: Until the YAML Loader learns about character positions, we hardcode 1 here (since this is all for traits on actor definitions).
						return new SpriteSequenceDefinition(x.Key, x.ParentNode.Key, filename ?? defaultFilename, loc);
					});

				yield return new SpriteSequenceImageDefinition(node.Key, location, sequences.ToArray());
			}
		}
	}
}
