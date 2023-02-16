using System.Collections.Generic;
using System.Linq;
using Oraide.Core.Entities.MiniYaml;

namespace Oraide.MiniYaml.Abstraction.SymbolGenerationStrategies
{
	public interface IMiniYamlSymbolGenerationStrategy
	{
		IEnumerable<YamlNode> ParseModFile(in string modFolder);

		IEnumerable<YamlNode> ParseMapFile(in string mapFolder);

		ILookup<string, ActorDefinition> GetActorDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods);

		ILookup<string, WeaponDefinition> GetWeaponDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods);

		ILookup<string, ConditionDefinition> GetConditionDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods);

		ILookup<string, CursorDefinition> GetCursorDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods);

		ILookup<string, PaletteDefinition> GetPaletteDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods, HashSet<string> knownPaletteTypes);

		ILookup<string, SpriteSequenceImageDefinition> GetSpriteSequenceDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods);

		(IEnumerable<YamlNode> Original, IEnumerable<YamlNode> Flattened) ParseText(string text, string fileUriString = null);
	}
}
