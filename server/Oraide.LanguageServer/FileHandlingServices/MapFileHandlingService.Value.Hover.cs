using System.Collections.Generic;
using System.IO;
using System.Linq;
using LspTypes;
using Oraide.Core;
using Oraide.Core.Entities.Csharp;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.FileHandlingServices;
using Oraide.LanguageServer.Caching;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class MapFileHandlingService
	{
		protected override Hover ValueHover(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetNodeIndentation switch
			{
				0 => HandleValueHoverAt0(cursorTarget),
				1 => HandleValueHoverAt1(cursorTarget),
				2 => HandleValueHoverAt2(cursorTarget),
				_ => null
			};
		}

		#region Private methods

		Hover HandleValueHoverAt0(CursorTarget cursorTarget)
		{
			if (cursorTarget.TargetNode.Key is "Rules" or "Sequences" or "ModelSequences" or "Weapons" or "Voices" or "Music" or "Notifications")
			{
				if (cursorTarget.TargetString.Contains('|'))
				{
					var resolvedFile = OpenRaFolderUtils.ResolveFilePath(cursorTarget.TargetString, (cursorTarget.ModId, symbolCache[cursorTarget.ModId].ModFolder));
					if (File.Exists(resolvedFile))
						return IHoverService.HoverFromHoverInfo($"```csharp\nFile \"{cursorTarget.TargetString}\"\n```", range);
				}
				else
				{
					var targetPath = cursorTarget.TargetStart.FileUri.AbsolutePath.Replace("file:///", string.Empty).Replace("%3A", ":");
					var mapFolder = Path.GetDirectoryName(targetPath);
					var mapName = Path.GetFileName(mapFolder);
					var filePath = Path.Combine(mapFolder, cursorTarget.TargetString);
					if (File.Exists(filePath))
						return IHoverService.HoverFromHoverInfo($"```csharp\nFile \"{mapName}/{cursorTarget.TargetString}\"\n```", range);
				}
			}

			return null; // TODO:
		}

		Hover HandleValueHoverAt1(CursorTarget cursorTarget)
		{
			if (cursorTarget.TargetNode?.ParentNode?.Key == "Actors")
			{
				// Actor definitions from the mod rules:
				if (modSymbols.ActorDefinitions.Contains(cursorTarget.TargetString))
					return IHoverService.HoverFromHoverInfo($"```csharp\nActor \"{cursorTarget.TargetString}\"\n```", range);

				// Actor definitions from map rules:
				var mapReference = symbolCache[cursorTarget.ModId].Maps
					.FirstOrDefault(x => x.MapFileReference == cursorTarget.FileReference);

				if (mapReference.MapReference != null && symbolCache.Maps.TryGetValue(mapReference.MapReference, out var mapSymbols))
					if (mapSymbols.ActorDefinitions.Contains(cursorTarget.TargetString))
						return IHoverService.HoverFromHoverInfo($"```csharp\nActor \"{cursorTarget.TargetString}\"\n```", range);
			}

			return null;
		}

		Hover HandleValueHoverAt2(CursorTarget cursorTarget)
		{
			if (cursorTarget.TargetNode?.ParentNode?.ParentNode?.Key == "Players")
			{
				// TODO: Add support for factions and for players.
			}

			if (cursorTarget.TargetNode?.ParentNode?.ParentNode?.Key == "Actors")
			{
				// TODO: Add support for ActorInits one day.
			}

			return null;
		}

		#endregion
	}
}
