using System.Collections.Generic;
using System.Linq;
using Oraide.Core.Entities;
using Oraide.Core.Entities.Csharp;
using Oraide.Core.Entities.MiniYaml;
using Oraide.Csharp;
using Oraide.MiniYaml;

namespace Oraide.LanguageServer.Caching
{
	// TODO: Make this mod-aware (and per-mod) instead of having dictionaries per mod. Then we can potentially also load/leave traits from loaded assemblies.
	public class SymbolCache
	{
		public string ModId { get; }

		public string ModFolder { get; }

		/// <summary>
		/// TraitInfo information grouped by TraitInfoName.
		/// </summary>
		// TODO: Populate this asynchronously from a separate thread because it can be very, very slow.
		public ILookup<string, TraitInfo> TraitInfos { get; private set; }

		/// <summary>
		/// A collection of all actor definitions in YAML (including abstract ones) grouped by their key/name.
		/// </summary>
		public IReadOnlyDictionary<string, ILookup<string, ActorDefinition>> ActorDefinitionsPerMod { get; private set; }

		/// <summary>
		/// A collection of all weapon definitions in YAML (including abstract ones) grouped by their key/name.
		/// </summary>
		public IReadOnlyDictionary<string, ILookup<string, WeaponDefinition>> WeaponDefinitionsPerMod { get; private set; }

		/// <summary>
		/// A collection of all granted and consumed conditions and their usages in YAML grouped by their name.
		/// </summary>
		public IReadOnlyDictionary<string, ILookup<string, MemberLocation>> ConditionDefinitionsPerMod { get; private set; }

		private readonly CodeInformationProvider codeInformationProvider;
		private readonly YamlInformationProvider yamlInformationProvider;

		public SymbolCache(string modId, string modFolder, ILookup<string, TraitInfo> traitInfos, ILookup<string, ActorDefinition> actorDefinitions,
			ILookup<string, WeaponDefinition> weaponDefinitions, ILookup<string, MemberLocation> conditionDefinitions)
		{
			ModId = modId;
			ModFolder = modFolder;
			ActorDefinitionsPerMod = new Dictionary<string, ILookup<string, ActorDefinition>>
			{
				{ modId, actorDefinitions }
			};

			WeaponDefinitionsPerMod = new Dictionary<string, ILookup<string, WeaponDefinition>>
			{
				{ modId, weaponDefinitions }
			};

			ConditionDefinitionsPerMod = new Dictionary<string, ILookup<string, MemberLocation>>
			{
				{ modId, conditionDefinitions }
			};
		}

		public SymbolCache(CodeInformationProvider codeInformationProvider, YamlInformationProvider yamlInformationProvider)
		{
			this.codeInformationProvider = codeInformationProvider;
			this.yamlInformationProvider = yamlInformationProvider;

			UpdateCodeSymbols();
			UpdateYamlSymbols();
		}

		// Intentionally synchronous code so the client can't continue working with a stale cache while we work on the update.
		// TODO: The way I see code symbol update happening is by the user manually triggering an update via an IDE command
		// that prompts the extension/client to notify the server to update, because neither the server nor the text editor can guarantee
		// that they would be watching the code files for changes.
		public void UpdateCodeSymbols()
		{
			TraitInfos = codeInformationProvider.GetTraitInfos();
		}

		public void UpdateYamlSymbols()
		{
			ActorDefinitionsPerMod = yamlInformationProvider.GetActorDefinitions();
			WeaponDefinitionsPerMod = yamlInformationProvider.GetWeaponDefinitions();
			ConditionDefinitionsPerMod = yamlInformationProvider.GetConditionDefinitions();
		}
	}
}
