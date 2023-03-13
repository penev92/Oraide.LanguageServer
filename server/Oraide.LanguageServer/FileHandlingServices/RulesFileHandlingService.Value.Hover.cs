using System.Linq;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.FileHandlingServices;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class RulesFileHandlingService
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
			if (cursorTarget.TargetNode.Key == "Inherits" || cursorTarget.TargetNode.Key.StartsWith("Inherits@"))
			{
				if (modSymbols.ActorDefinitions.Contains(cursorTarget.TargetString))
					return IHoverService.HoverFromHoverInfo($"```csharp\nActor \"{cursorTarget.TargetString}\"\n```", range);

				if (cursorTarget.FileType == FileType.MapRules)
				{
					var mapReference = symbolCache[cursorTarget.ModId].Maps
						.FirstOrDefault(x => x.RulesFiles.Contains(cursorTarget.FileReference));

					if (mapReference.MapReference != null && symbolCache.Maps.TryGetValue(mapReference.MapReference, out var mapSymbols))
						if (mapSymbols.ActorDefinitions.Contains(cursorTarget.TargetString))
							return IHoverService.HoverFromHoverInfo($"```csharp\nActor \"{cursorTarget.TargetString}\"\n```", range);
				}
			}

			return null;
		}

		Hover HandleValueHoverAt2(CursorTarget cursorTarget)
		{
			var traitName = cursorTarget.TargetNode.ParentNode.Key.Split('@')[0];

			// Using .First() is not great but we have no way to differentiate between traits of the same name
			// until the server learns the concept of a mod and loaded assemblies.
			var traitInfo = codeSymbols.TraitInfos[traitName].FirstOrDefault();
			if (traitInfo.Name == null)
				return null;

			var fieldInfo = traitInfo.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.Key);
			if (fieldInfo.Name == null)
				return null;

			var actorDefinitions = modSymbols.ActorDefinitions.Select(x => x.Key);
			var weaponDefinitions = modSymbols.WeaponDefinitions.Select(x => x.Key);
			var conditionDefinitions = modSymbols.ConditionDefinitions.Select(x => x.Key);
			var cursorDefinitions = modSymbols.CursorDefinitions.Select(x => x.Key);
			var paletteDefinitions = modSymbols.PaletteDefinitions;
			var spriteSequenceImageDefinitions = modSymbols.SpriteSequenceImageDefinitions;

			MapManifest mapManifest = default;
			if (cursorTarget.FileType == FileType.MapRules)
			{
				mapManifest = symbolCache[cursorTarget.ModId].Maps
					.FirstOrDefault(x => x.RulesFiles.Contains(cursorTarget.FileReference));

				if (mapManifest.MapReference != null && symbolCache.Maps.TryGetValue(mapManifest.MapReference, out var mapSymbols))
				{
					actorDefinitions = actorDefinitions.Union(mapSymbols.ActorDefinitions.Select(x => x.Key));
					weaponDefinitions = weaponDefinitions.Union(mapSymbols.WeaponDefinitions.Select(x => x.Key));
					conditionDefinitions = conditionDefinitions.Union(mapSymbols.ConditionDefinitions.Select(x => x.Key));

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

			if (fieldInfo.OtherAttributes.Any(x => x.Name == "ActorReference") && actorDefinitions.Contains(cursorTarget.TargetString))
				return IHoverService.HoverFromHoverInfo($"```csharp\nActor \"{cursorTarget.TargetString}\"\n```", range);

			if (fieldInfo.OtherAttributes.Any(x => x.Name == "WeaponReference") && weaponDefinitions.Contains(cursorTarget.TargetString))
				return IHoverService.HoverFromHoverInfo($"```csharp\nWeapon \"{cursorTarget.TargetString}\"\n```", range);

			if (fieldInfo.OtherAttributes.Any(x => x.Name == "GrantedConditionReference") && conditionDefinitions.Contains(cursorTarget.TargetString))
				return IHoverService.HoverFromHoverInfo($"```csharp\nCondition \"{cursorTarget.TargetString}\"\n```", range);

			if (fieldInfo.OtherAttributes.Any(x => x.Name == "ConsumedConditionReference") && conditionDefinitions.Contains(cursorTarget.TargetString))
				return IHoverService.HoverFromHoverInfo($"```csharp\nCondition \"{cursorTarget.TargetString}\"\n```", range);

			if (fieldInfo.OtherAttributes.Any(x => x.Name == "CursorReference") && cursorDefinitions.Contains(cursorTarget.TargetString))
			{
				// Maps can't define cursors, so this is fine using mod symbols only.
				var cursor = modSymbols.CursorDefinitions[cursorTarget.TargetString].First();
				return IHoverService.HoverFromHoverInfo(cursor.ToMarkdownInfoString(), range);
			}

			if (fieldInfo.OtherAttributes.Any(x => x.Name == "PaletteReference") && paletteDefinitions.Contains(cursorTarget.TargetString))
			{
				var palette = paletteDefinitions[cursorTarget.TargetString].First();
				return IHoverService.HoverFromHoverInfo(palette.ToMarkdownInfoString(), range);
			}

			// Pretend there is such a thing as a "SequenceImageReferenceAttribute" until we add it in OpenRA one day.
			// NOTE: This will improve if/when we add the attribute.
			if (traitInfo.PropertyInfos.Any(x => x.OtherAttributes.Any(y => y.Name == "SequenceReference"
					&& !string.IsNullOrWhiteSpace(y.Value)
					&& (y.Value.Contains(',') ? y.Value.Substring(0, y.Value.IndexOf(',')) == fieldInfo.Name : y.Value == fieldInfo.Name)))
				&& spriteSequenceImageDefinitions.Contains(cursorTarget.TargetString))
			{
				var image = spriteSequenceImageDefinitions[cursorTarget.TargetString].First();
				return IHoverService.HoverFromHoverInfo(image.ToMarkdownInfoString(), range);
			}

			if (fieldInfo.OtherAttributes.Any(x => x.Name == "SequenceReference"))
			{
				var imageName = ResolveSpriteSequenceImageNameForRules(cursorTarget, fieldInfo, mapManifest);
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
