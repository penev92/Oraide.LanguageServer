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
		protected override IEnumerable<CompletionItem> ValueCompletion(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetNodeIndentation switch
			{
				0 => Enumerable.Empty<CompletionItem>(),
				1 => HandleValueCompletionAt1(cursorTarget),
				2 => HandleValueCompletionAt2(cursorTarget),
				_ => Enumerable.Empty<CompletionItem>()
			};
		}

		#region Private methods

		IEnumerable<CompletionItem> HandleValueCompletionAt1(CursorTarget cursorTarget)
		{
			var nodeKey = cursorTarget.TargetNode.Key;

			// Get weapon definitions (for inheriting).
			if (nodeKey == "Inherits" || nodeKey.StartsWith("Inherits@"))
			{
				if (cursorTarget.FileType == FileType.MapWeapons)
				{
					var mapReference = symbolCache[cursorTarget.ModId].Maps
						.FirstOrDefault(x => x.WeaponsFiles.Contains(cursorTarget.FileReference));

					if (mapReference.MapReference != null && symbolCache.Maps.TryGetValue(mapReference.MapReference, out var mapSymbols))
						return weaponNames.Union(mapSymbols.WeaponDefinitions.Select(x => x.First().ToCompletionItem()));
				}

				return weaponNames;
			}

			if (nodeKey == "Projectile")
				return weaponInfo.ProjectileInfos.Where(x => !x.IsAbstract).Select(x => x.ToCompletionItem("Type implementing IProjectileInfo"));

			if (nodeKey == "Warhead" || nodeKey.StartsWith("Warhead@"))
				return weaponInfo.WarheadInfos.Where(x => !x.IsAbstract).Select(x => x.ToCompletionItem("Type implementing IWarhead"));

			return Enumerable.Empty<CompletionItem>();
		}

		IEnumerable<CompletionItem> HandleValueCompletionAt2(CursorTarget cursorTarget)
		{
			ClassFieldInfo fieldInfo = default;
			var classFieldInfos = Array.Empty<ClassFieldInfo>();
			var parentNode = cursorTarget.TargetNode.ParentNode;
			if (parentNode.Key == "Projectile")
			{
				var projectileInfo = weaponInfo.ProjectileInfos.FirstOrDefault(x => x.Name == parentNode.Value);
				if (projectileInfo.Name != null)
				{
					fieldInfo = projectileInfo.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.Key);
					classFieldInfos = projectileInfo.PropertyInfos;
				}
			}
			else if (parentNode.Key == "Warhead" || parentNode.Key.StartsWith("Warhead@"))
			{
				var warheadInfo = weaponInfo.WarheadInfos.FirstOrDefault(x => x.Name == parentNode.Value);
				if (warheadInfo.Name != null)
				{
					fieldInfo = warheadInfo.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.Key);
					classFieldInfos = warheadInfo.PropertyInfos;
				}
			}

			MapManifest mapManifest = default;
			if (cursorTarget.FileType == FileType.MapRules)
			{
				mapManifest = symbolCache[cursorTarget.ModId].Maps
					.FirstOrDefault(x => x.RulesFiles.Contains(cursorTarget.FileReference));

				if (mapManifest.MapReference != null && symbolCache.Maps.TryGetValue(mapManifest.MapReference, out var mapSymbols))
				{
					paletteNames = paletteNames.Union(mapSymbols.PaletteDefinitions.Select(x => x.First().ToCompletionItem()));
					spriteSequenceImageNames = spriteSequenceImageNames.Union(
						mapSymbols.SpriteSequenceImageDefinitions.Select(x => x.First().ToCompletionItem()));
				}
			}

			if (fieldInfo.OtherAttributes.Any(x => x.Name == "PaletteReference"))
				return paletteNames.Where(x => !string.IsNullOrEmpty(x.Label));

			// Pretend there is such a thing as a "SequenceImageReferenceAttribute" until we add it in OpenRA one day.
			// NOTE: This will improve if/when we add the attribute.
			if (classFieldInfos.Any(x => x.OtherAttributes.Any(y => y.Name == "SequenceReference"
					&& (y.Value.Contains(',') ? y.Value.Substring(0, y.Value.IndexOf(',')) == fieldInfo.Name : y.Value == fieldInfo.Name))))
			{
				return spriteSequenceImageNames;
			}

			if (fieldInfo.OtherAttributes.Any(x => x.Name == "SequenceReference"))
			{
				// Resolve sequence image inheritance so we can show all inherited sequences.
				var imageName = ResolveSpriteSequenceImageNameForWeapons(cursorTarget, fieldInfo, mapManifest);
				var sequences = GetSpriteSequencesForImage(cursorTarget.ModId, imageName, mapManifest);
				return sequences.Select(x => x.ToCompletionItem());
			}

			// Try to check if this is an enum type field.
			var enumInfo = symbolCache[cursorTarget.ModId].CodeSymbols.EnumInfos.FirstOrDefault(x => x.Key == fieldInfo.InternalType);
			if (enumInfo != null)
			{
				return enumInfo.FirstOrDefault().Values.Select(x => new CompletionItem
				{
					Label = x,
					Kind = CompletionItemKind.EnumMember,
					Detail = "Enum type value",
					Documentation = $"{enumInfo.Key}.{x}"
				});
			}

			if (fieldInfo.InternalType == "bool")
			{
				return new[] { CompletionItems.True, CompletionItems.False };
			}

			return Enumerable.Empty<CompletionItem>();
		}

		#endregion
	}
}
