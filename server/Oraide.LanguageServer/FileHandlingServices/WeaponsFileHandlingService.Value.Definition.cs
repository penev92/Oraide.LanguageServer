using System;
using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.Core.Entities.Csharp;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Extensions;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class WeaponsFileHandlingService
	{
		protected override IEnumerable<Location> ValueDefinition(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetNodeIndentation switch
			{
				0 => Enumerable.Empty<Location>(),
				1 => HandleValueDefinitionAt1(cursorTarget),
				2 => HandleValueDefinitionAt2(cursorTarget),
				_ => Enumerable.Empty<Location>()
			};
		}

		#region Private methods

		IEnumerable<Location> HandleValueDefinitionAt1(CursorTarget cursorTarget)
		{
			var targetNodeKey = cursorTarget.TargetNode.Key;
			if (targetNodeKey == "Inherits")
			{
				var weaponDefinitions = modSymbols.WeaponDefinitions.FirstOrDefault(x => x.Key == cursorTarget.TargetString);
				if (weaponDefinitions != null)
					return weaponDefinitions.Select(x => x.Location.ToLspLocation(weaponDefinitions.Key.Length));

				if (cursorTarget.FileType == FileType.MapWeapons)
				{
					var mapReference = symbolCache[cursorTarget.ModId].Maps
						.FirstOrDefault(x => x.WeaponsFiles.Contains(cursorTarget.FileReference));

					if (mapReference.MapReference != null && symbolCache.Maps.TryGetValue(mapReference.MapReference, out var mapSymbols))
						if (mapSymbols.WeaponDefinitions.Contains(cursorTarget.TargetString))
							return mapSymbols.WeaponDefinitions[cursorTarget.TargetString].Select(x => x.Location.ToLspLocation(x.Name.Length));
				}
			}
			else if (targetNodeKey == "Projectile")
			{
				var projectile = codeSymbols.WeaponInfo.ProjectileInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetString);
				if (projectile.Name != null)
					return new[] { projectile.Location.ToLspLocation(projectile.Name.Length) };
			}
			else if (targetNodeKey == "Warhead" || targetNodeKey.StartsWith("Warhead@"))
			{
				var warhead = codeSymbols.WeaponInfo.WarheadInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetString);
				if (warhead.Name != null)
					return new[] { warhead.Location.ToLspLocation(warhead.Name.Length) };
			}

			return Enumerable.Empty<Location>();
		}

		IEnumerable<Location> HandleValueDefinitionAt2(CursorTarget cursorTarget)
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

			var paletteDefinitions = modSymbols.PaletteDefinitions[cursorTarget.TargetString];
			var spriteSequenceImageDefinitions = symbolCache[cursorTarget.ModId].ModSymbols.SpriteSequenceImageDefinitions;

			MapManifest mapManifest = default;
			if (cursorTarget.FileType == FileType.MapRules)
			{
				mapManifest = symbolCache[cursorTarget.ModId].Maps
					.FirstOrDefault(x => x.RulesFiles.Contains(cursorTarget.FileReference));

				if (mapManifest.MapReference != null && symbolCache.Maps.TryGetValue(mapManifest.MapReference, out var mapSymbols))
				{
					// Merge mod symbols with map symbols.
					paletteDefinitions = paletteDefinitions.Union(mapSymbols.PaletteDefinitions[cursorTarget.TargetString]);
					spriteSequenceImageDefinitions = spriteSequenceImageDefinitions
						.SelectMany(x => x)
						.Union(mapSymbols.SpriteSequenceImageDefinitions.SelectMany(x => x))
						.ToLookup(x => x.Name, y => y);
				}
			}

			if (fieldInfo.OtherAttributes.Any(x => x.Name == "PaletteReference"))
				return paletteDefinitions.Select(x => x.Location.ToLspLocation(x.Type.Length));

			// Pretend there is such a thing as a "SequenceImageReferenceAttribute" until we add it in OpenRA one day.
			// NOTE: This will improve if/when we add the attribute.
			if (fieldInfos.Any(x => x.OtherAttributes.Any(y => y.Name == "SequenceReference"
					&& (y.Value.Contains(',') ? y.Value.Substring(0, y.Value.IndexOf(',')) == fieldInfo.Name : y.Value == fieldInfo.Name))))
			{
				return spriteSequenceImageDefinitions[cursorTarget.TargetString].Select(x => x.Location.ToLspLocation(x.Name.Length));
			}

			if (fieldInfo.OtherAttributes.Any(x => x.Name == "SequenceReference"))
			{
				var imageName = ResolveSpriteSequenceImageNameForWeapons(cursorTarget, fieldInfo, mapManifest);
				return spriteSequenceImageDefinitions[imageName].SelectMany(x => x.Sequences)
					.Where(x => x.Name == cursorTarget.TargetString)
					.Select(x => x.Location.ToLspLocation(x.Name.Length));
			}

			// Try to check if this is an enum type field.
			var enumInfo = codeSymbols.EnumInfos.FirstOrDefault(x => x.Key == fieldInfo.InternalType);
			if (enumInfo != null)
			{
				return new[] { enumInfo.First().Location.ToLspLocation(enumInfo.Key.Length) };
			}

			return Enumerable.Empty<Location>();
		}

		#endregion
	}
}
