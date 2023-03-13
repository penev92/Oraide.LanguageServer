using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.Core;
using Oraide.Core.Entities.MiniYaml;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class ModFileHandlingService
	{
		// Copied from OpenRA's Manifest.
		readonly string[] reservedModuleNames =
		{
			"Include", "Metadata", "Folders", "MapFolders", "Packages", "Rules",
			"Sequences", "ModelSequences", "Cursors", "Chrome", "Assemblies", "ChromeLayout", "Weapons",
			"Voices", "Notifications", "Music", "Translations", "TileSets", "ChromeMetrics", "Missions", "Hotkeys",
			"ServerTraits", "LoadScreen", "DefaultOrderGenerator", "SupportsMapsFrom", "SoundFormats", "SpriteFormats", "VideoFormats",
			"RequiresMods", "PackageFormats"
		};

		protected override IEnumerable<CompletionItem> KeyCompletion(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetNodeIndentation switch
			{
				0 => HandleKeyCompletionAt0(cursorTarget),
				_ => Enumerable.Empty<CompletionItem>()
			};
		}

		#region Private methods

		IEnumerable<CompletionItem> HandleKeyCompletionAt0(CursorTarget cursorTarget)
		{
			// TODO: This is missing all the IGlobalMetadata implementations.
			var fileUri = OpenRaFolderUtils.ResolveFilePath(cursorTarget.FileReference, symbolCache.KnownMods);
			var presentModules = openFileCache[fileUri.ToString()].YamlNodes.Select(x => x.Key);
			var missingModules = reservedModuleNames.Except(presentModules);
			return missingModules.Select(x => new CompletionItem
			{
				Label = x,
				Kind = CompletionItemKind.Snippet,
				CommitCharacters = new[] { ":" }
			});
		}

		#endregion
	}
}
