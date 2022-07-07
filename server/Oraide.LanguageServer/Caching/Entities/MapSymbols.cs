using System.Linq;
using Oraide.Core.Entities.MiniYaml;

namespace Oraide.LanguageServer.Caching.Entities
{
	public class MapSymbols
	{
		/// <summary>
		/// A collection of all actor definitions in the current map's YAML (including abstract ones) grouped by their key/name.
		/// </summary>
		public ILookup<string, ActorDefinition> ActorDefinitions { get; }

		/// <summary>
		/// A collection of all weapon definitions in the current map's YAML (including abstract ones) grouped by their key/name.
		/// </summary>
		public ILookup<string, WeaponDefinition> WeaponDefinitions { get; }

		/// <summary>
		/// A collection of all granted and consumed conditions and their usages in the current map's YAML grouped by their name.
		/// </summary>
		public ILookup<string, ConditionDefinition> ConditionDefinitions { get; }

		/// <summary>
		/// A collection of all palette definitions in the current map's YAML grouped by their name.
		/// </summary>
		public ILookup<string, PaletteDefinition> PaletteDefinitions { get; }

		public MapSymbols(ILookup<string, ActorDefinition> actorDefinitions, ILookup<string, WeaponDefinition> weaponDefinitions,
			ILookup<string, ConditionDefinition> conditionDefinitions, ILookup<string, PaletteDefinition> paletteDefinitions)
		{
			ActorDefinitions = actorDefinitions;
			WeaponDefinitions = weaponDefinitions;
			ConditionDefinitions = conditionDefinitions;
			PaletteDefinitions = paletteDefinitions;
		}
	}
}
