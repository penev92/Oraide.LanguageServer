using System.Linq;
using Oraide.Core.Entities;
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
		public ILookup<string, MemberLocation> ConditionDefinitions { get; }

		public ModSymbols(string modId, string modFolder, ILookup<string, TraitInfo> traitInfos, ILookup<string, ActorDefinition> actorDefinitions,
			ILookup<string, WeaponDefinition> weaponDefinitions, ILookup<string, MemberLocation> conditionDefinitions)
		{
			ModId = modId;
			ModFolder = modFolder;
			TraitInfos = traitInfos;
			ActorDefinitions = actorDefinitions;
			WeaponDefinitions = weaponDefinitions;
			ConditionDefinitions = conditionDefinitions;
		}
	}
}
