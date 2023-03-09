using System.Linq;
using LspTypes;
using Oraide.Core;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.FileHandlingServices;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class RulesFileHandlingService
	{
		protected override Hover KeyHover(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetNodeIndentation switch
			{
				0 => HandleKeyHoverAt0(cursorTarget),
				1 => HandleKeyHoverAt1(cursorTarget),
				2 => HandleKeyHoverAt2(cursorTarget),
				_ => null
			};
		}

		#region Private methods

		Hover HandleKeyHoverAt0(CursorTarget cursorTarget)
		{
			if (modSymbols.ActorDefinitions.Contains(cursorTarget.TargetString))
			{
				string content;
				if (cursorTarget.TargetString.StartsWith('^'))
					content = $"```csharp\nAbstract Actor \"{cursorTarget.TargetString}\"\n```\n" +
					          $"Abstract actor definitions are meant to be inherited and will not be considered as real actors by the game.";
				else
					content = $"```csharp\nActor \"{cursorTarget.TargetString}\"\n```";

				return IHoverService.HoverFromHoverInfo(content, range);
			}

			if (cursorTarget.FileType == FileType.MapRules)
			{
				var mapReference = symbolCache[cursorTarget.ModId].Maps
					.FirstOrDefault(x => x.RulesFiles.Contains(cursorTarget.FileReference));

				if (mapReference.MapReference != null && symbolCache.Maps.TryGetValue(mapReference.MapReference, out var mapSymbols))
					if (mapSymbols.ActorDefinitions.Contains(cursorTarget.TargetString))
						return IHoverService.HoverFromHoverInfo($"```csharp\nActor \"{cursorTarget.TargetString}\"\n```", range);
			}

			return null;
		}

		Hover HandleKeyHoverAt1(CursorTarget cursorTarget)
		{
			if (cursorTarget.TargetString == "Inherits")
				return IHoverService.HoverFromHoverInfo($"Inherits (and possibly overwrites) rules from actor {cursorTarget.TargetNode.Value}", range);

			var traitInfoName = $"{cursorTarget.TargetString}Info";
			if (codeSymbols.TraitInfos.Contains(traitInfoName))
			{
				// Using .First() is not great but we have no way to differentiate between traits of the same name
				// until the server learns the concept of a mod and loaded assemblies.
				var traitInfo = codeSymbols.TraitInfos[traitInfoName].First();
				var content = traitInfo.ToMarkdownInfoString() + "\n\n" + "https://docs.openra.net/en/release/traits/#" + $"{traitInfo.Name.ToLower()}";
				return IHoverService.HoverFromHoverInfo(content, range);
			}

			if (cursorTarget.TargetString[0] == '-')
			{
				traitInfoName = traitInfoName.Substring(1);
				if (codeSymbols.TraitInfos.Contains(traitInfoName))
				{
					var modData = symbolCache[cursorTarget.ModId];
					var fileList = modData.ModManifest.RulesFiles;
					var resolvedFileList = fileList.Select(x => OpenRaFolderUtils.ResolveFilePath(x, (modData.ModId, modData.ModFolder)));

					if (TryMergeYamlFiles(resolvedFileList, out _))
						return IHoverService.HoverFromHoverInfo($"Removes trait `{cursorTarget.TargetString.Substring(1)}` from the actor.", range);
				}
			}

			return null;
		}

		// TODO: This will likely not handle trait property removals properly!
		Hover HandleKeyHoverAt2(CursorTarget cursorTarget)
		{
			var traitInfoName = $"{cursorTarget.TargetNode.ParentNode.Key.Split("@")[0]}Info";
			if (codeSymbols.TraitInfos.Contains(traitInfoName))
			{
				// Using .First() is not great but we have no way to differentiate between traits of the same name
				// until the server learns the concept of a mod and loaded assemblies.
				var traitInfo = codeSymbols.TraitInfos[traitInfoName].First();
				var fieldInfo = traitInfo.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetString);
				var content = fieldInfo.ToMarkdownInfoString();
				return IHoverService.HoverFromHoverInfo(content, range);
			}

			return null;
		}

		#endregion
	}
}
