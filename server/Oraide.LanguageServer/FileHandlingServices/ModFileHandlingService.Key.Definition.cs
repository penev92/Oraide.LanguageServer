using System.Collections.Generic;
using System.IO;
using System.Linq;
using LspTypes;
using Oraide.Core;
using Oraide.Core.Entities.MiniYaml;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class ModFileHandlingService
	{
		protected override IEnumerable<Location> KeyDefinition(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetNodeIndentation switch
			{
				0 => Enumerable.Empty<Location>(),
				1 => HandleKeyDefinitionAt1(cursorTarget),
				_ => Enumerable.Empty<Location>()
			};
		}

		#region Private methods

		IEnumerable<Location> HandleKeyDefinitionAt1(CursorTarget cursorTarget)
		{
			// Copy of MapFileHandlingService's file reference handling.
			if (cursorTarget.TargetNode.ParentNode.Key is "Rules" or "Weapons" or "Sequences" or "ModelSequences" or "Cursors" or "TileSets"
			    or "Chrome" or "ChromeLayout" or "ChromeMetrics" or "Translations" or "Voices" or "Notifications" or "Music" or "Hotkeys" or "Missions")
			{
				var resolvedFile = OpenRaFolderUtils.ResolveFilePath(cursorTarget.TargetString, symbolCache.KnownMods);
				if (File.Exists(resolvedFile.AbsolutePath))
					return new[]
					{
						new Location
						{
							Uri = resolvedFile.ToString(),
							Range = new LspTypes.Range
							{
								Start = new Position(0, 0),
								End = new Position(0, 0)
							}
						}
					};
			}

			return Enumerable.Empty<Location>();
		}

		#endregion
	}
}
