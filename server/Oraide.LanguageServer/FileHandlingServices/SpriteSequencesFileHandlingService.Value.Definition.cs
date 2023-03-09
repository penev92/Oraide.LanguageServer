using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.FileHandlingServices;
using Oraide.LanguageServer.Extensions;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class SpriteSequencesFileHandlingService : BaseFileHandlingService
	{
		// NOTE: Copied from HandleWeaponsFileKey.
		protected override IEnumerable<Location> ValueDefinition(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetNodeIndentation switch
			{
				0 => Enumerable.Empty<Location>(),
				1 => HandleValueDefinitionAt1(cursorTarget),
				2 => HandleValueDefinitionAt2(cursorTarget),
				3 => Enumerable.Empty<Location>(),
				4 => HandleValueDefinitionAt4(cursorTarget),
				_ => Enumerable.Empty<Location>()
			};
		}

		#region Private methods

		IEnumerable<Location> HandleValueDefinitionAt1(CursorTarget cursorTarget)
		{
			if (cursorTarget.TargetNode.Key == "Inherits")
			{
				if (modSymbols.SpriteSequenceImageDefinitions.Contains(cursorTarget.TargetString))
					return modSymbols.SpriteSequenceImageDefinitions[cursorTarget.TargetString].Select(x => x.Location.ToLspLocation(x.Name.Length));

				if (cursorTarget.FileType == FileType.MapRules)
				{
					var mapReference = symbolCache[cursorTarget.ModId].Maps
						.FirstOrDefault(x => x.SpriteSequenceFiles.Contains(cursorTarget.FileReference));

					if (mapReference.MapReference != null && symbolCache.Maps.TryGetValue(mapReference.MapReference, out var mapSymbols))
						if (mapSymbols.SpriteSequenceImageDefinitions.Contains(cursorTarget.TargetString))
							return mapSymbols.SpriteSequenceImageDefinitions[cursorTarget.TargetString].Select(x => x.Location.ToLspLocation(x.Name.Length));
				}
			}

			return Enumerable.Empty<Location>();
		}

		IEnumerable<Location> HandleValueDefinitionAt2(CursorTarget cursorTarget)
		{
			return HandleSpriteSequencePropertyValueDefinition(cursorTarget);
		}

		IEnumerable<Location> HandleValueDefinitionAt4(CursorTarget cursorTarget)
		{
			if (cursorTarget.TargetNode.ParentNode.ParentNode.Key == "Combine")
				return HandleSpriteSequencePropertyValueDefinition(cursorTarget);

			return Enumerable.Empty<Location>();
		}

		IEnumerable<Location> HandleSpriteSequencePropertyValueDefinition(CursorTarget cursorTarget)
		{
			var spriteSequenceFormat = symbolCache[cursorTarget.ModId].ModManifest.SpriteSequenceFormat.Type;
			var spriteSequenceType = symbolCache[cursorTarget.ModId].CodeSymbols.SpriteSequenceInfos[spriteSequenceFormat].First();

			var fieldInfo = spriteSequenceType.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.Key);
			if (fieldInfo.Name != null)
			{
				// Try to check if this is an enum type field.
				var enumInfo = symbolCache[cursorTarget.ModId].CodeSymbols.EnumInfos
					.FirstOrDefault(x => x.Key == fieldInfo.InternalType);
				if (enumInfo != null)
				{
					return new[] { enumInfo.First().Location.ToLspLocation(enumInfo.Key.Length) };
				}
			}

			return Enumerable.Empty<Location>();
		}

		#endregion
	}
}
