using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Oraide.Core.Entities;
using Oraide.Core.Entities.Csharp;
using Oraide.Core.Entities.MiniYaml;
using Oraide.Csharp;
using Oraide.MiniYaml;

namespace Oraide.LanguageServer
{
	public class SymbolCache
	{
		// With the addition of other collections dedicated to holding definitions and this being delegated to a lookup table for client cursor position,
		// this is now redundant and should be replaced with on-demand parsing of the current file or with a cache that handles didOpen/didChange/didSave.
		// No point in loading everything up-front and also it could get stale really fast.
		public readonly IReadOnlyDictionary<string, ReadOnlyCollection<YamlNode>> ParsedRulesPerFile;

		// TODO: Change to ILookup (to match `actorDefinitions` and also there may be more than one trait with the same name across namespaces).
		// TODO: Populate this asynchronously from a separate thread because it can be very, very slow.
		public IReadOnlyDictionary<string, TraitInfo> TraitInfos { get; private set; }

		/// <summary>
		/// A collection of all actor definitions in YAML (including abstract ones) grouped by their key/name.
		/// </summary>
		public ILookup<string, ActorDefinition> ActorDefinitions { get; private set; }

		/// <summary>
		/// A collection of all granted and consumed conditions and their usages in YAML grouped by their name.
		/// </summary>
		public ILookup<string, MemberLocation> ConditionDefinitions { get; private set; }

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
			ParsedRulesPerFile = yamlInformationProvider.GetParsedRulesPerFile();
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
			ActorDefinitions = yamlInformationProvider.GetActorDefinitions();
			ConditionDefinitions = yamlInformationProvider.GetConditionDefinitions();
		}
	}
}
