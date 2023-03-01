using System.Collections.Generic;
using Oraide.Core.Entities.MiniYaml;

namespace Oraide.MiniYaml.Abstraction.Parsers
{
	public interface IMiniYamlParser
	{
		bool CanParse(in string folderPath);

		IEnumerable<YamlNode> ParseModFile(in string modFolder);

		IEnumerable<YamlNode> ParseMapFile(in string mapFolder);

		IEnumerable<ActorDefinition> ParseActorDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods);

		IEnumerable<WeaponDefinition> ParseWeaponDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods);

		IEnumerable<ConditionDefinition> ParseConditionDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods);

		IEnumerable<CursorDefinition> ParseCursorDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods);

		IEnumerable<PaletteDefinition> ParsePaletteDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods, HashSet<string> knownPaletteTypes);

		IEnumerable<SpriteSequenceImageDefinition> ParseSpriteSequenceDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods);

		(IEnumerable<YamlNode> Original, IEnumerable<YamlNode> Flattened) ParseText(string text, string fileUriString = null);
	}
}
