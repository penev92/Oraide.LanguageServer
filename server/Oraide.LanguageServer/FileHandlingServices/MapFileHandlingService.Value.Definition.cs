using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LspTypes;
using Oraide.Core;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Extensions;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class MapFileHandlingService
	{
		protected override IEnumerable<Location> ValueDefinition(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetNodeIndentation switch
			{
				0 => HandleValueDefinitionAt0(cursorTarget),
				1 => HandleValueDefinitionAt1(cursorTarget),
				_ => Enumerable.Empty<Location>()
			};
		}

		#region Private methods

		IEnumerable<Location> HandleValueDefinitionAt0(CursorTarget cursorTarget)
		{
			string filePath = null;
			if (cursorTarget.TargetNode.Key is "Rules" or "Sequences" or "ModelSequences" or "Weapons" or "Voices" or "Music" or "Notifications")
			{
				if (cursorTarget.TargetString.Contains('|'))
				{
					filePath = OpenRaFolderUtils.ResolveFilePath(cursorTarget.TargetString, (cursorTarget.ModId, symbolCache[cursorTarget.ModId].ModFolder));
				}
				else
				{
					var targetPath = cursorTarget.TargetStart.FileUri.AbsolutePath;
					var mapFolder = Path.GetDirectoryName(targetPath);
					filePath = Path.Combine(mapFolder, cursorTarget.TargetString);
				}
			}

			if (filePath != null && File.Exists(filePath))
			{
				return new[]
				{
					new Location
					{
						Uri = new Uri(filePath).ToString(),
						Range = new LspTypes.Range
						{
							Start = new Position(0, 0),
							End = new Position(0, 0)
						}
					}
				};
			}

			return null;
		}

		IEnumerable<Location> HandleValueDefinitionAt1(CursorTarget cursorTarget)
		{
			if (cursorTarget.TargetNode?.ParentNode?.Key == "Actors")
			{
				// Actor definitions from the mod rules:
				if (modSymbols.ActorDefinitions.Contains(cursorTarget.TargetString))
					return modSymbols.ActorDefinitions[cursorTarget.TargetString].Select(x => x.Location.ToLspLocation(x.Name.Length));

				// Actor definitions from map rules:
				var mapReference = symbolCache[cursorTarget.ModId].Maps
					.FirstOrDefault(x => x.MapFileReference == cursorTarget.FileReference);

				if (mapReference.MapReference != null && symbolCache.Maps.TryGetValue(mapReference.MapReference, out var mapSymbols))
					if (mapSymbols.ActorDefinitions.Contains(cursorTarget.TargetString))
						return mapSymbols.ActorDefinitions[cursorTarget.TargetString].Select(x => x.Location.ToLspLocation(x.Name.Length));
			}

			return Enumerable.Empty<Location>();
		}

		#endregion
	}
}
