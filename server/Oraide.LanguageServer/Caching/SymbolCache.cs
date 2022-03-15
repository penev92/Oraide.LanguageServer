﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Oraide.Core;
using Oraide.Core.Entities.MiniYaml;
using Oraide.Csharp;
using Oraide.LanguageServer.Caching.Entities;
using Oraide.MiniYaml;

namespace Oraide.LanguageServer.Caching
{
	public class SymbolCache
	{
		public IReadOnlyDictionary<string, ModData> ModSymbols { get; private set; }

		private readonly CodeInformationProvider codeInformationProvider;
		private readonly YamlInformationProvider yamlInformationProvider;

		public SymbolCache(CodeInformationProvider codeInformationProvider, YamlInformationProvider yamlInformationProvider)
		{
			this.codeInformationProvider = codeInformationProvider;
			this.yamlInformationProvider = yamlInformationProvider;

			Update();
		}

		public void Update()
		{
			ModSymbols = CreateSymbolCachesPerMod();
		}

		IReadOnlyDictionary<string, ModData> CreateSymbolCachesPerMod()
		{
			var modFolders = yamlInformationProvider.GetModDirectories();
			var mods = modFolders.ToDictionary(OpenRaFolderUtils.GetModId, y => y);

			// TODO: Remove this flex when the code is stable and we're sure it won't need optimizing.
			Console.Error.WriteLine($"Found {mods.Count} mod(s): {string.Join(", ", mods.Keys)}.");
			Console.Error.WriteLine("Start loading symbol information...");
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			// Intentionally synchronous code so the client can't continue working with a stale cache while we work on the update.
			// TODO: The way I see code symbol update happening is by the user manually triggering an update via an IDE command
			// that prompts the extension/client to notify the server to update, because neither the server nor the text editor can guarantee
			// that they would be watching the code files for changes.
			var traitInfos = codeInformationProvider.GetTraitInfos();
			var weaponInfo = codeInformationProvider.GetWeaponInfo();

			var elapsed = stopwatch.Elapsed;
			Console.Error.WriteLine($"Took {elapsed} to load {traitInfos.Count} traitInfos, {weaponInfo.ProjectileInfos.Length} projectileInfos and {weaponInfo.WarheadInfos.Length} warheadInfos.");

			var actorDefinitionsPerMod = yamlInformationProvider.GetActorDefinitions();
			var weaponDefinitionsPerMod = yamlInformationProvider.GetWeaponDefinitions();
			var conditionDefinitionsPerMod = yamlInformationProvider.GetConditionDefinitions();
			var cursorDefinitionsPerMod = yamlInformationProvider.GetCursorDefinitions();

			elapsed = stopwatch.Elapsed;
			Console.Error.WriteLine($"Took {elapsed} to load everything.");

			return mods.Select(x =>
			{
				var codeSymbols = new CodeSymbols(traitInfos, weaponInfo);
				var modSymbols = new ModSymbols(
					actorDefinitionsPerMod.ContainsKey(x.Key)
						? actorDefinitionsPerMod[x.Key]
						: Array.Empty<ActorDefinition>().ToLookup(y => y.Name, z => z),
					weaponDefinitionsPerMod.ContainsKey(x.Key)
						? weaponDefinitionsPerMod[x.Key]
						: Array.Empty<WeaponDefinition>().ToLookup(y => y.Name, z => z),
					conditionDefinitionsPerMod.ContainsKey(x.Key)
						? conditionDefinitionsPerMod[x.Key]
						: Array.Empty<ConditionDefinition>().ToLookup(y => y.Name, z => z),
					cursorDefinitionsPerMod.ContainsKey(x.Key)
						? cursorDefinitionsPerMod[x.Key]
						: Array.Empty<CursorDefinition>().ToLookup(y => y.Name, z => z));

				return new ModData(x.Key, x.Value, modSymbols, codeSymbols);
			}).ToDictionary(x => x.ModId, y => y);
		}

		public ModData this[string key] => ModSymbols[key];
	}
}
