using System.Linq;
using Oraide.Core.Entities.MiniYaml;

namespace Oraide.LanguageServer.Caching.Entities
{
	public class ModSymbols
	{
		/// <summary>
		/// A collection of all actor definitions in YAML (including abstract ones) grouped by their key/name.
		/// </summary>
		public ILookup<string, ActorDefinition> ActorDefinitions { get; }

		/// <summary>
		/// A collection of all weapon definitions in YAML (including abstract ones) grouped by their key/name.
		/// </summary>
		public ILookup<string, WeaponDefinition> WeaponDefinitions { get; }

		/// <summary>
		/// A collection of all granted and consumed conditions and their usages in YAML grouped by their name.
		/// </summary>
		public ILookup<string, ConditionDefinition> ConditionDefinitions { get; }

		/// <summary>
		/// A collection of all defined cursors.
		/// </summary>
		public ILookup<string, CursorDefinition> CursorDefinitions { get; }

		/// <summary>
		/// A collection of all palette definitions.
		/// </summary>
		public ILookup<string, PaletteDefinition> PaletteDefinitions { get; }

		public ModSymbols(ILookup<string, ActorDefinition> actorDefinitions, ILookup<string, WeaponDefinition> weaponDefinitions,
			ILookup<string, ConditionDefinition> conditionDefinitions, ILookup<string, CursorDefinition> cursorDefinitions,
			ILookup<string, PaletteDefinition> paletteDefinitions)
		{
			ActorDefinitions = actorDefinitions;
			WeaponDefinitions = weaponDefinitions;
			ConditionDefinitions = conditionDefinitions;
			CursorDefinitions = cursorDefinitions;
			PaletteDefinitions = paletteDefinitions;
		}
	}
}
