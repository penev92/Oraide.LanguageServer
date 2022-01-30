using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Oraide.Core;
using Oraide.Core.Entities;
using Oraide.Core.Entities.MiniYaml;
using Oraide.Csharp;
using Oraide.MiniYaml;

namespace Oraide.LanguageServer.Caching
{
	public class SymbolCacheFactory
	{
		private readonly CodeInformationProvider codeInformationProvider;
		private readonly YamlInformationProvider yamlInformationProvider;

		public SymbolCacheFactory(CodeInformationProvider codeInformationProvider, YamlInformationProvider yamlInformationProvider)
		{
			this.codeInformationProvider = codeInformationProvider;
			this.yamlInformationProvider = yamlInformationProvider;
		}

		public IReadOnlyDictionary<string, SymbolCache> CreateSymbolCachesPerMod()
		{
			var modFolders = yamlInformationProvider.GetModDirectories();
			var mods = modFolders.ToDictionary(OpenRaFolderUtils.GetModId, y => y);

			// TODO: Remove this flex when the code is stable and we're sure it won't need optimizing.
			Console.Error.WriteLine("Start loading symbol information...");
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var traitInfos = codeInformationProvider.GetTraitInfos();

			var elapsed = stopwatch.Elapsed;
			Console.Error.WriteLine($"Loaded {traitInfos.Count} traitInfos in {elapsed}.");

			var actorDefinitionsPerMod = yamlInformationProvider.GetActorDefinitions();
			var weaponDefinitionsPerMod = yamlInformationProvider.GetWeaponDefinitions();
			var conditionDefinitionsPerMod = yamlInformationProvider.GetConditionDefinitions();

			elapsed = stopwatch.Elapsed;
			Console.Error.WriteLine($"Loaded everything in {elapsed}.");

			return mods.Select(x =>
			{
				return new SymbolCache(x.Key, x.Value,
					traitInfos,
					actorDefinitionsPerMod.ContainsKey(x.Key)
						? actorDefinitionsPerMod[x.Key]
						: Array.Empty<ActorDefinition>().ToLookup(y => y.Name, z => z),
					weaponDefinitionsPerMod.ContainsKey(x.Key)
						? weaponDefinitionsPerMod[x.Key]
						: Array.Empty<WeaponDefinition>().ToLookup(y => y.Name, z => z),
					conditionDefinitionsPerMod.ContainsKey(x.Key)
						? conditionDefinitionsPerMod[x.Key]
						: Array.Empty<MemberLocation>().ToLookup(y => string.Empty, z => z));
			}).ToDictionary(x => x.ModId, y => y);
		}
	}
}
