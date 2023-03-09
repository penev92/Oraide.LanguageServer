using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Extensions;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class RulesFileHandlingService
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
			// Get actor definitions (for inheriting).
			if (cursorTarget.TargetNode.Key == "Inherits" || cursorTarget.TargetNode.Key.StartsWith("Inherits@"))
			{
				if (modSymbols.ActorDefinitions.Contains(cursorTarget.TargetString))
					return modSymbols.ActorDefinitions[cursorTarget.TargetString].Select(x => x.Location.ToLspLocation(x.Name.Length));

				if (cursorTarget.FileType == FileType.MapRules)
				{
					var mapReference = symbolCache[cursorTarget.ModId].Maps
						.FirstOrDefault(x => x.RulesFiles.Contains(cursorTarget.FileReference));

					if (mapReference.MapReference != null && symbolCache.Maps.TryGetValue(mapReference.MapReference, out var mapSymbols))
						if (mapSymbols.ActorDefinitions.Contains(cursorTarget.TargetString))
							return mapSymbols.ActorDefinitions[cursorTarget.TargetString].Select(x => x.Location.ToLspLocation(x.Name.Length));
				}
			}

			return Enumerable.Empty<Location>();
		}

		IEnumerable<Location> HandleValueDefinitionAt2(CursorTarget cursorTarget)
		{
			var traitName = cursorTarget.TargetNode.ParentNode.Key.Split('@')[0];
			var traitInfoName = $"{traitName}Info";

			// Using .First() is not great but we have no way to differentiate between traits of the same name
			// until the server learns the concept of a mod and loaded assemblies.
			var traitInfo = codeSymbols.TraitInfos[traitInfoName].FirstOrDefault();
			if (traitInfo.Name == null)
				return Enumerable.Empty<Location>();

			var fieldInfo = traitInfo.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.Key);
			if (fieldInfo.Name == null)
				return Enumerable.Empty<Location>();

			var actorDefinitions = modSymbols.ActorDefinitions[cursorTarget.TargetString];
			var weaponDefinitions = modSymbols.WeaponDefinitions[cursorTarget.TargetString];
			var conditionDefinitions = modSymbols.ConditionDefinitions[cursorTarget.TargetString];
			var cursorDefinitions = modSymbols.CursorDefinitions[cursorTarget.TargetString];
			var paletteDefinitions = modSymbols.PaletteDefinitions[cursorTarget.TargetString];
			var spriteSequenceImageDefinitions = modSymbols.SpriteSequenceImageDefinitions;

			MapManifest mapManifest = default;
			if (cursorTarget.FileType == FileType.MapRules)
			{
				mapManifest = symbolCache[cursorTarget.ModId].Maps
					.FirstOrDefault(x => x.RulesFiles.Contains(cursorTarget.FileReference));

				if (mapManifest.MapReference != null && symbolCache.Maps.TryGetValue(mapManifest.MapReference, out var mapSymbols))
				{
					actorDefinitions = actorDefinitions.Union(mapSymbols.ActorDefinitions[cursorTarget.TargetString]);
					weaponDefinitions = weaponDefinitions.Union(mapSymbols.WeaponDefinitions[cursorTarget.TargetString]);
					conditionDefinitions = conditionDefinitions.Union(mapSymbols.ConditionDefinitions[cursorTarget.TargetString]);
					paletteDefinitions = paletteDefinitions.Union(mapSymbols.PaletteDefinitions[cursorTarget.TargetString]);

					spriteSequenceImageDefinitions = spriteSequenceImageDefinitions
						.SelectMany(x => x)
						.Union(mapSymbols.SpriteSequenceImageDefinitions.SelectMany(x => x))
						.ToLookup(x => x.Name, y => y);
				}
			}

			if (fieldInfo.OtherAttributes.Any(x => x.Name == "ActorReference"))
				return actorDefinitions.Select(x => x.Location.ToLspLocation(x.Name.Length));

			if (fieldInfo.OtherAttributes.Any(x => x.Name == "WeaponReference"))
				return weaponDefinitions.Select(x => x.Location.ToLspLocation(x.Name.Length));

			if (fieldInfo.OtherAttributes.Any(x => x.Name == "GrantedConditionReference"))
				return conditionDefinitions.Select(x => x.Location.ToLspLocation(x.Name.Length));

			if (fieldInfo.OtherAttributes.Any(x => x.Name == "ConsumedConditionReference"))
				return conditionDefinitions.Select(x => x.Location.ToLspLocation(x.Name.Length));

			if (fieldInfo.OtherAttributes.Any(x => x.Name == "CursorReference"))
				return cursorDefinitions.Select(x => x.Location.ToLspLocation(x.Name.Length));

			if (fieldInfo.OtherAttributes.Any(x => x.Name == "PaletteReference"))
				return paletteDefinitions.Select(x => x.Location.ToLspLocation(x.Type.Length));

			// Pretend there is such a thing as a "SequenceImageReferenceAttribute" until we add it in OpenRA one day.
			// NOTE: This will improve if/when we add the attribute.
			if (traitInfo.PropertyInfos.Any(x => x.OtherAttributes.Any(y => y.Name == "SequenceReference"
					&& !string.IsNullOrWhiteSpace(y.Value)
					&& (y.Value.Contains(',') ? y.Value.Substring(0, y.Value.IndexOf(',')) == fieldInfo.Name : y.Value == fieldInfo.Name))))
				return spriteSequenceImageDefinitions[cursorTarget.TargetString].Select(x => x.Location.ToLspLocation(x.Name.Length));

			if (fieldInfo.OtherAttributes.Any(x => x.Name == "SequenceReference"))
			{
				var imageName = ResolveSpriteSequenceImageNameForRules(cursorTarget, fieldInfo, mapManifest);
				return spriteSequenceImageDefinitions[imageName].SelectMany(x => x.Sequences)
					.Where(x => x.Name == cursorTarget.TargetString)
					.Select(x => x.Location.ToLspLocation(x.Name.Length));
			}

			// Try to check if this is an enum type field.
			var enumInfo = codeSymbols.EnumInfos.FirstOrDefault(x => x.Key == fieldInfo.InternalType);
			if (enumInfo != null)
				return new[] { enumInfo.First().Location.ToLspLocation(enumInfo.Key.Length) };

			return Enumerable.Empty<Location>();
		}

		#endregion
	}
}
