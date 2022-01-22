using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Oraide.Core.Entities;
using Oraide.Core.Entities.Csharp;
using Oraide.Core.Entities.MiniYaml;
using Oraide.Csharp;
using Oraide.MiniYaml;

namespace Oraide.LanguageServer.Caching
{
	public class SymbolCache
	{
		// TODO: Change to ILookup (to match `actorDefinitions` and also there may be more than one trait with the same name across namespaces).
		// TODO: Populate this asynchronously from a separate thread because it can be very, very slow.
		public IReadOnlyDictionary<string, TraitInfo> TraitInfos { get; private set; }

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

		public SymbolCache(string workspaceFolderPath, string defaultOpenRaFolderPath)
		{
			Console.Error.WriteLine("WORKSPACE FOLDER PATH:");
			Console.Error.WriteLine(workspaceFolderPath);
			Console.Error.WriteLine("OPENRA FOLDER PATH:");
			Console.Error.WriteLine(defaultOpenRaFolderPath);
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			// TODO: Get this via DI:
			codeInformationProvider = new CodeInformationProvider(workspaceFolderPath, defaultOpenRaFolderPath);
			UpdateCodeSymbols();

			var elapsed = stopwatch.Elapsed;
			Console.Error.WriteLine($"Loaded {TraitInfos.Count} traitInfos in {elapsed}.");

			// TODO: Get this via DI:
			yamlInformationProvider = new YamlInformationProvider(workspaceFolderPath);
			UpdateYamlSymbols();

			elapsed = stopwatch.Elapsed;
			Console.Error.WriteLine($"Loaded everything in {elapsed}.");
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
