using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Extensions;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class RulesFileHandlingService
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
			// Get actor definitions (for inheriting).
			if (cursorTarget.TargetNode.Key == "Inherits" || cursorTarget.TargetNode.Key.StartsWith("Inherits@"))
			{
				if (cursorTarget.FileType == FileType.MapRules)
				{
					var mapReference = symbolCache[cursorTarget.ModId].Maps
						.FirstOrDefault(x => x.RulesFiles.Contains(cursorTarget.FileReference));

					if (mapReference.MapReference != null && symbolCache.Maps.TryGetValue(mapReference.MapReference, out var mapSymbols))
						return actorNames.Union(mapSymbols.ActorDefinitions.Select(x => x.First().ToCompletionItem()));
				}

				return actorNames;
			}

			return Enumerable.Empty<CompletionItem>();
		}

		IEnumerable<CompletionItem> HandleValueCompletionAt2(CursorTarget cursorTarget)
		{
			var traitName = cursorTarget.TargetNode.ParentNode.Key.Split('@')[0];
			var traitInfoName = $"{traitName}Info";

			// Using .First() is not great but we have no way to differentiate between traits of the same name
			// until the server learns the concept of a mod and loaded assemblies.
			var traitInfo = codeSymbols.TraitInfos[traitInfoName].FirstOrDefault();
			if (traitInfo.Name == null)
				return Enumerable.Empty<CompletionItem>();

			var fieldInfo = traitInfo.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.Key);
			if (fieldInfo.Name == null)
				return Enumerable.Empty<CompletionItem>();

			var tempActorNames = actorNames;
			var tempWeaponNames = weaponNames;
			var tempConditionNames = conditionNames;
			var tempCursorNames = cursorNames;
			var tempPaletteNames = paletteNames;
			var tempSpriteSequenceImageNames = spriteSequenceImageNames;

			MapManifest mapManifest = default;
			if (cursorTarget.FileType == FileType.MapRules)
			{
				mapManifest = symbolCache[cursorTarget.ModId].Maps
					.FirstOrDefault(x => x.RulesFiles.Contains(cursorTarget.FileReference));

				if (mapManifest.MapReference != null && symbolCache.Maps.TryGetValue(mapManifest.MapReference, out var mapSymbols))
				{
					// TODO: Don't map to everything CompletionItems here! Defer that until we know what we need, then only map that (like in DefinitionHandler).
					tempActorNames = tempActorNames.Union(mapSymbols.ActorDefinitions.Select(x => x.First().ToCompletionItem()));
					tempWeaponNames = tempWeaponNames.Union(mapSymbols.WeaponDefinitions.Select(x => x.First().ToCompletionItem()));
					tempConditionNames = tempConditionNames.Union(mapSymbols.ConditionDefinitions.Select(x => x.First().ToCompletionItem()));
					tempPaletteNames = tempPaletteNames.Union(mapSymbols.PaletteDefinitions.Select(x => x.First().ToCompletionItem()));
					tempSpriteSequenceImageNames = tempSpriteSequenceImageNames.Union(
						mapSymbols.SpriteSequenceImageDefinitions.Select(x => x.First().ToCompletionItem()));
				}
			}

			if (fieldInfo.OtherAttributes.Any(x => x.Name == "ActorReference"))
				return tempActorNames;

			if (fieldInfo.OtherAttributes.Any(x => x.Name == "WeaponReference"))
				return tempWeaponNames;

			if (fieldInfo.OtherAttributes.Any(x => x.Name == "GrantedConditionReference"))
				return tempConditionNames;

			if (fieldInfo.OtherAttributes.Any(x => x.Name == "ConsumedConditionReference"))
				return tempConditionNames;

			if (fieldInfo.OtherAttributes.Any(x => x.Name == "CursorReference"))
				return tempCursorNames;

			if (fieldInfo.OtherAttributes.Any(x => x.Name == "PaletteReference"))
				return tempPaletteNames.Where(x => !string.IsNullOrEmpty(x.Label));

			// Pretend there is such a thing as a "SequenceImageReferenceAttribute" until we add it in OpenRA one day.
			// NOTE: This will improve if/when we add the attribute.
			if (traitInfo.PropertyInfos.Any(x => x.OtherAttributes.Any(y => y.Name == "SequenceReference"
					&& !string.IsNullOrWhiteSpace(y.Value)
					&& (y.Value.Contains(',') ? y.Value.Substring(0, y.Value.IndexOf(',')) == fieldInfo.Name : y.Value == fieldInfo.Name))))
				return tempSpriteSequenceImageNames;

			if (fieldInfo.OtherAttributes.Any(x => x.Name == "SequenceReference"))
			{
				// Resolve sequence image inheritance so we can show all inherited sequences.
				var imageName = ResolveSpriteSequenceImageNameForRules(cursorTarget, fieldInfo, mapManifest);
				var sequences = GetSpriteSequencesForImage(cursorTarget.ModId, imageName, mapManifest);
				return sequences.Select(x => x.ToCompletionItem());
			}

			// Try to check if this is an enum type field.
			var enumInfo = codeSymbols.EnumInfos.FirstOrDefault(x => x.Key == fieldInfo.InternalType);
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
				return new[] { CompletionItems.True, CompletionItems.False };

			return Enumerable.Empty<CompletionItem>();
		}

		#endregion
	}
}
