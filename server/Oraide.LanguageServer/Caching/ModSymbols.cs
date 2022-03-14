using System.Linq;
using Oraide.Core.Entities.Csharp;
using Oraide.Core.Entities.MiniYaml;

namespace Oraide.LanguageServer.Caching
{
	public class ModSymbols
	{
		public string ModId { get; }

		public string ModFolder { get; }

		/// <summary>
		/// TraitInfo information grouped by TraitInfoName.
		/// </summary>
		// TODO: Populate this asynchronously from a separate thread because it can be very, very slow.
		public ILookup<string, TraitInfo> TraitInfos { get; }

		/// <summary>
		/// Information about the WeaponInfo class, all IProjectileInfo implementations and all IWarhead implementations.
		/// </summary>
		public WeaponInfo WeaponInfo { get; }

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

		public ModSymbols(string modId, string modFolder, ILookup<string, TraitInfo> traitInfos, WeaponInfo weaponInfo,
			ILookup<string, ActorDefinition> actorDefinitions, ILookup<string, WeaponDefinition> weaponDefinitions,
			ILookup<string, ConditionDefinition> conditionDefinitions, ILookup<string, CursorDefinition> cursorDefinitions)
		{
			ModId = modId;
			ModFolder = modFolder;
			TraitInfos = traitInfos;
			WeaponInfo = weaponInfo;
			ActorDefinitions = actorDefinitions;
			WeaponDefinitions = weaponDefinitions;
			ConditionDefinitions = conditionDefinitions;
			CursorDefinitions = cursorDefinitions;
		}
	}
}
