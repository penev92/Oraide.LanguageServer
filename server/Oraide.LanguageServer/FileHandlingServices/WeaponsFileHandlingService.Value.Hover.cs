using System;
using System.Linq;
using LspTypes;
using Oraide.Core.Entities.Csharp;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.FileHandlingServices;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class WeaponsFileHandlingService
	{
		protected override Hover ValueHover(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetNodeIndentation switch
			{
				0 => null,
				1 => HandleValueHoverAt1(cursorTarget),
				2 => HandleValueHoverAt2(cursorTarget),
				_ => null
			};
		}

		#region Private methods

		Hover HandleValueHoverAt1(CursorTarget cursorTarget)
		{
			var nodeKey = cursorTarget.TargetNode.Key;

			if (nodeKey == "Inherits")
			{
				if (modSymbols.WeaponDefinitions.Any(x => x.Key == cursorTarget.TargetString))
					return IHoverService.HoverFromHoverInfo($"```csharp\nWeapon \"{cursorTarget.TargetString}\"\n```", range);

				if (cursorTarget.FileType == FileType.MapWeapons)
				{
					var mapReference = symbolCache[cursorTarget.ModId].Maps
						.FirstOrDefault(x => x.WeaponsFiles.Contains(cursorTarget.FileReference));

					if (mapReference.MapReference != null && symbolCache.Maps.TryGetValue(mapReference.MapReference, out var mapSymbols))
						if (mapSymbols.WeaponDefinitions.Contains(cursorTarget.TargetString))
							return IHoverService.HoverFromHoverInfo($"```csharp\nWeapon \"{cursorTarget.TargetString}\"\n```", range);
				}
			}
			else if (nodeKey == "Projectile")
			{
				var projectileInfo = codeSymbols.WeaponInfo.ProjectileInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetString);
				if (projectileInfo.Name != null)
				{
					var content = projectileInfo.ToMarkdownInfoString() +
								  "\n\n" + "https://docs.openra.net/en/release/weapons/#" + $"{projectileInfo.Name.ToLower()}";

					return IHoverService.HoverFromHoverInfo(content, range);
				}
			}
			else if (nodeKey == "Warhead" || nodeKey.StartsWith("Warhead@"))
			{
				var warheadInfo = codeSymbols.WeaponInfo.WarheadInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetString);
				if (warheadInfo.Name != null)
				{
					var content = warheadInfo.ToMarkdownInfoString() +
								  "\n\n" + "https://docs.openra.net/en/release/weapons/#" + $"{warheadInfo.InfoName.ToLower()}";

					return IHoverService.HoverFromHoverInfo(content, range);
				}
			}

			var fieldInfo = codeSymbols.WeaponInfo.WeaponPropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.Key);

			// Show explanation for world range value.
			if (fieldInfo.InternalType == "WDist")
			{
				var whole = 0;
				var parts = cursorTarget.TargetString.Split('c');
				if ((parts.Length == 1 && int.TryParse(parts[0], out var fraction))
					|| (parts.Length == 2 && int.TryParse(parts[0], out whole) && int.TryParse(parts[1], out fraction)))
				{
					var content = $"Range in world distance units equal to {whole} cells and {fraction} distance units (where 1 cell has 1024 units)";
					return IHoverService.HoverFromHoverInfo(content, range);
				}
			}

			return null;
		}

		Hover HandleValueHoverAt2(CursorTarget cursorTarget)
		{
			ClassFieldInfo fieldInfo = default;
			var fieldInfos = Array.Empty<ClassFieldInfo>();
			var parentNode = cursorTarget.TargetNode.ParentNode;
			if (parentNode.Key == "Projectile")
			{
				var projectileInfo = codeSymbols.WeaponInfo.ProjectileInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.ParentNode.Value);
				if (projectileInfo.Name != null)
				{
					fieldInfo = projectileInfo.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.Key);
					fieldInfos = projectileInfo.PropertyInfos;
				}
			}
			else if (parentNode.Key == "Warhead" || parentNode.Key.StartsWith("Warhead@"))
			{
				var warheadInfo = codeSymbols.WeaponInfo.WarheadInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.ParentNode.Value);
				if (warheadInfo.Name != null)
				{
					fieldInfo = warheadInfo.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.Key);
					fieldInfos = warheadInfo.PropertyInfos;
				}
			}

			var paletteDefinitions = modSymbols.PaletteDefinitions;
			var spriteSequenceImageDefinitions = modSymbols.SpriteSequenceImageDefinitions;

			MapManifest mapManifest = default;
			if (cursorTarget.FileType == FileType.MapRules)
			{
				mapManifest = symbolCache[cursorTarget.ModId].Maps
					.FirstOrDefault(x => x.RulesFiles.Contains(cursorTarget.FileReference));

				if (mapManifest.MapReference != null && symbolCache.Maps.TryGetValue(mapManifest.MapReference, out var mapSymbols))
				{
					// Merge mod symbols with map symbols.
					paletteDefinitions = paletteDefinitions
						.SelectMany(x => x)
						.Union(mapSymbols.PaletteDefinitions.SelectMany(x => x))
						.ToLookup(x => x.Name, y => y);
					spriteSequenceImageDefinitions = spriteSequenceImageDefinitions
						.SelectMany(x => x)
						.Union(mapSymbols.SpriteSequenceImageDefinitions.SelectMany(x => x))
						.ToLookup(x => x.Name, y => y);
				}
			}

			if (fieldInfo.OtherAttributes.Any(x => x.Name == "PaletteReference") && paletteDefinitions.Contains(cursorTarget.TargetString))
			{
				var palette = paletteDefinitions[cursorTarget.TargetString].First();
				return IHoverService.HoverFromHoverInfo(palette.ToMarkdownInfoString(), range);
			}

			// Pretend there is such a thing as a "SequenceImageReferenceAttribute" until we add it in OpenRA one day.
			// NOTE: This will improve if/when we add the attribute.
			if (fieldInfos.Any(x => x.OtherAttributes.Any(y => y.Name == "SequenceReference"
					&& (y.Value.Contains(',') ? y.Value.Substring(0, y.Value.IndexOf(',')) == fieldInfo.Name : y.Value == fieldInfo.Name)))
				&& spriteSequenceImageDefinitions.Contains(cursorTarget.TargetString))
			{
				var image = spriteSequenceImageDefinitions[cursorTarget.TargetString].First();
				return IHoverService.HoverFromHoverInfo(image.ToMarkdownInfoString(), range);
			}

			if (fieldInfo.OtherAttributes.Any(x => x.Name == "SequenceReference"))
			{
				var imageName = ResolveSpriteSequenceImageNameForWeapons(cursorTarget, fieldInfo, mapManifest);
				var image = spriteSequenceImageDefinitions[imageName].First();
				var spriteSequence = image.Sequences.First(x => x.Name == cursorTarget.TargetString);
				return IHoverService.HoverFromHoverInfo(spriteSequence.ToMarkdownInfoString(), range);
			}

			// Try to check if this is an enum type field.
			var enumInfo = symbolCache[cursorTarget.ModId].CodeSymbols.EnumInfos.FirstOrDefault(x => x.Key == fieldInfo.InternalType);
			if (enumInfo != null)
			{
				var content = $"```csharp\n{enumInfo.Key}.{cursorTarget.TargetString}\n```";
				return IHoverService.HoverFromHoverInfo(content, range);
			}

			// Show explanation for world range value.
			if (fieldInfo.InternalType == "WDist")
			{
				var whole = 0;
				var parts = cursorTarget.TargetString.Split('c');
				if ((parts.Length == 1 && int.TryParse(parts[0], out var fraction))
					|| (parts.Length == 2 && int.TryParse(parts[0], out whole) && int.TryParse(parts[1], out fraction)))
				{
					var content = $"Range in world distance units equal to {whole} cells and {fraction} distance units (where 1 cell has 1024 units)";
					return IHoverService.HoverFromHoverInfo(content, range);
				}
			}

			return null;
		}

		#endregion
	}
}
