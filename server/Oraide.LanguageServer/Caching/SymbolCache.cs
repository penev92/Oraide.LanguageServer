using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

		public IDictionary<string, MapSymbols> Maps { get; } = new Dictionary<string, MapSymbols>();

		public string CodeVersion => codeInformationProvider.CodeVersion;
		public string YamlVersion => yamlInformationProvider.YamlVersion;

		readonly CodeInformationProvider codeInformationProvider;
		readonly YamlInformationProvider yamlInformationProvider;

		HashSet<string> knownPaletteTypes;

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
			var modFolders = yamlInformationProvider.GetModDirectories().Select(OpenRaFolderUtils.NormalizeFilePathString);
			var mods = modFolders.ToDictionary(OpenRaFolderUtils.GetModId, y => y);

			// TODO: Remove this flex when the code is stable and we're sure it won't need optimizing.
			Console.Error.WriteLine($"Found {mods.Count} mod(s): {string.Join(", ", mods.Keys)}.");
			Console.Error.WriteLine("Start loading symbol information...");
			var stopwatchTotal = new Stopwatch();
			stopwatchTotal.Start();

			// Intentionally synchronous code so the client can't continue working with a stale cache while we work on the update.
			// TODO: The way I see code symbol update happening is by the user manually triggering an update via an IDE command
			// that prompts the extension/client to notify the server to update, because neither the server nor the text editor can guarantee
			// that they would be watching the code files for changes.
			var traitInfos = codeInformationProvider.GetTraitInfos();
			var weaponInfo = codeInformationProvider.GetWeaponInfo();
			var paletteTraitInfos = codeInformationProvider.GetPaletteTraitInfos();
			var spriteSequenceInfos = codeInformationProvider.GetSpriteSequenceInfos();
			var enumInfos = codeInformationProvider.GetEnumInfos();

			var codeSymbols = new CodeSymbols(traitInfos, paletteTraitInfos, weaponInfo, spriteSequenceInfos, enumInfos);

			var elapsedTotal = stopwatchTotal.Elapsed;
			Console.Error.WriteLine($"Took {elapsedTotal} to load code symbols:");
			Console.Error.WriteLine($"    {traitInfos.Count} traitInfos");
			Console.Error.WriteLine($"    {weaponInfo.ProjectileInfos.Length} projectileInfos");
			Console.Error.WriteLine($"    {weaponInfo.WarheadInfos.Length} warheadInfos");
			Console.Error.WriteLine($"    {paletteTraitInfos.Count} paletteTraitInfos");
			Console.Error.WriteLine($"    {spriteSequenceInfos.Count} spriteSequenceInfos");
			Console.Error.WriteLine($"    {enumInfos.Count} enumInfos");

			var stopwatchYaml = new Stopwatch();
			stopwatchYaml.Start();

			var modDataPerMod = new Dictionary<string, ModData>();

			knownPaletteTypes = paletteTraitInfos.Select(x => x.FirstOrDefault().Name).ToHashSet();
			foreach (var modId in mods.Keys)
			{
				var modFolder = mods[modId];

				var modFileNodes = yamlInformationProvider.ReadModFile(modFolder);
				var modManifest = new ModManifest(modFileNodes);

				var actorDefinitions = yamlInformationProvider.GetActorDefinitions(modManifest.RulesFiles, mods);
				var weaponDefinitions = yamlInformationProvider.GetWeaponDefinitions(modManifest.WeaponsFiles, mods);
				var conditionDefinitions = yamlInformationProvider.GetConditionDefinitions(modManifest.RulesFiles, mods);
				var cursorDefinitions = yamlInformationProvider.GetCursorDefinitions(modManifest.CursorsFiles, mods);
				var paletteDefinitions = yamlInformationProvider.GetPaletteDefinitions(modManifest.RulesFiles, mods, knownPaletteTypes);
				var spriteSequenceDefinitions = yamlInformationProvider.GetSpriteSequenceDefinitions(modManifest.SpriteSequences, mods);

				var modSymbols = new ModSymbols(actorDefinitions, weaponDefinitions, conditionDefinitions, cursorDefinitions, paletteDefinitions, spriteSequenceDefinitions);

				var mapsDir = OpenRaFolderUtils.ResolveFilePath(modManifest.MapsFolder, mods);
				var allMaps = mapsDir == null
					? Enumerable.Empty<string>()
					: Directory.EnumerateDirectories(OpenRaFolderUtils.NormalizeFilePathString(mapsDir.AbsolutePath))
						.Select(OpenRaFolderUtils.NormalizeFilePathString);

				var mapDirs = allMaps.Where(x => File.Exists(Path.Combine(x, "map.yaml")) && File.Exists(Path.Combine(x, "map.bin"))).ToArray();
				var maps = mapDirs.Select(x => new MapManifest(x, yamlInformationProvider.ReadMapFile(x), modManifest.MapsFolder));

				modDataPerMod.Add(modId, new ModData(modId, modFolder, modManifest, modSymbols, codeSymbols, maps.ToArray()));
			}

			var elapsed = stopwatchYaml.Elapsed;
			elapsedTotal = stopwatchTotal.Elapsed;
			Console.Error.WriteLine($"Took {elapsed} to load mod YAML symbols.");

			Console.Error.WriteLine($"Took {elapsedTotal} to load everything.");

			return modDataPerMod;
		}

		public ModData this[string key] => ModSymbols[key];

		public void AddMap(string modId, MapManifest mapManifest)
		{
			var mods = new Dictionary<string, string> { { modId, ModSymbols[modId].ModFolder } };
			var actorDefinitions = yamlInformationProvider.GetActorDefinitions(mapManifest.RulesFiles, mods);
			var weaponDefinitions = yamlInformationProvider.GetWeaponDefinitions(mapManifest.WeaponsFiles, mods);
			var conditionDefinitions = yamlInformationProvider.GetConditionDefinitions(mapManifest.RulesFiles, mods);
			var paletteDefinitions = yamlInformationProvider.GetPaletteDefinitions(mapManifest.RulesFiles, mods, knownPaletteTypes);
			var spriteSequenceImageDefinitions = yamlInformationProvider.GetSpriteSequenceDefinitions(mapManifest.SpriteSequenceFiles, mods);

			var mapSymbols = new MapSymbols(actorDefinitions, weaponDefinitions, conditionDefinitions, paletteDefinitions, spriteSequenceImageDefinitions);
			Maps.Add(mapManifest.MapReference, mapSymbols);
		}

		public void UpdateMap(string modId, MapManifest mapManifest)
		{
			if (!Maps.ContainsKey(mapManifest.MapReference) || !Maps.Remove(mapManifest.MapReference))
				return;

			AddMap(modId, mapManifest);
		}
	}
}
