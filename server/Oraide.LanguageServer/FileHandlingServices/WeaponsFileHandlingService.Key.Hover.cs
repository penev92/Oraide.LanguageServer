using System.Linq;
using LspTypes;
using Oraide.Core;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.FileHandlingServices;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class WeaponsFileHandlingService
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
			if (modSymbols.WeaponDefinitions.Contains(cursorTarget.TargetString))
			{
				string content;
				if (cursorTarget.TargetString.StartsWith('^'))
					content = $"```csharp\nAbstract Weapon \"{cursorTarget.TargetString}\"\n```\n" +
					          $"Abstract weapon definitions are meant to be inherited and can not be used directly.";
				else
					content = $"```csharp\nWeapon \"{cursorTarget.TargetString}\"\n```";

				return IHoverService.HoverFromHoverInfo(content, range);
			}

			if (cursorTarget.FileType == FileType.MapWeapons)
			{
				var mapReference = symbolCache[cursorTarget.ModId].Maps
					.FirstOrDefault(x => x.WeaponsFiles.Contains(cursorTarget.FileReference));

				if (mapReference.MapReference != null && symbolCache.Maps.TryGetValue(mapReference.MapReference, out var mapSymbols))
					if (mapSymbols.WeaponDefinitions.Contains(cursorTarget.TargetString))
						return IHoverService.HoverFromHoverInfo($"```csharp\nWeapon \"{cursorTarget.TargetString}\"\n```", range);
			}

			return null;
		}

		Hover HandleKeyHoverAt1(CursorTarget cursorTarget)
		{
			if (cursorTarget.TargetString == "Inherits")
				return IHoverService.HoverFromHoverInfo($"Inherits (and possibly overwrites) rules from weapon {cursorTarget.TargetNode.Value}", range);

			if (cursorTarget.TargetString == "Warhead" || cursorTarget.TargetString.StartsWith("Warhead@"))
				return IHoverService.HoverFromHoverInfo("Warhead used by this weapon.", range);

			// Maybe this is a property of WeaponInfo.
			var prop = codeSymbols.WeaponInfo.WeaponPropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetString);
			if (prop.Name != null)
				return IHoverService.HoverFromHoverInfo(prop.ToMarkdownInfoString(), range);

			if (cursorTarget.TargetString[0] == '-')
			{
				var modData = symbolCache[cursorTarget.ModId];
				var fileList = modData.ModManifest.WeaponsFiles;
				var resolvedFileList = fileList.Select(x => OpenRaFolderUtils.ResolveFilePath(x, (modData.ModId, modData.ModFolder)));

				if (TryMergeYamlFiles(resolvedFileList, out _))
					return IHoverService.HoverFromHoverInfo($"Removes `{cursorTarget.TargetNode.Key.Substring(1)}` from the weapon.", range);
			}

			return null;
		}

		Hover HandleKeyHoverAt2(CursorTarget cursorTarget)
		{
			var parentNode = cursorTarget.TargetNode.ParentNode;
			if (parentNode.Key == "Projectile")
			{
				var projectileInfo = codeSymbols.WeaponInfo.ProjectileInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.ParentNode.Value);
				if (projectileInfo.Name != null)
				{
					var prop = projectileInfo.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetString);
					if (prop.Name != null)
						return IHoverService.HoverFromHoverInfo(prop.ToMarkdownInfoString(), range);
				}
			}
			else if (parentNode.Key == "Warhead" || parentNode.Key.StartsWith("Warhead@"))
			{
				var warheadInfo = codeSymbols.WeaponInfo.WarheadInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.ParentNode.Value);
				if (warheadInfo.Name != null)
				{
					var prop = warheadInfo.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetString);
					if (prop.Name != null)
						return IHoverService.HoverFromHoverInfo(prop.ToMarkdownInfoString(), range);
				}
			}

			return null;
		}

		#endregion
	}
}
