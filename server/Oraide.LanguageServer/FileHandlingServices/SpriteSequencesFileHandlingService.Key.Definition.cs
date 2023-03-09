using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Extensions;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class SpriteSequencesFileHandlingService
	{
		// NOTE: Copied from HandleWeaponsFileKey.
		protected override IEnumerable<Location> KeyDefinition(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetNodeIndentation switch
			{
				0 => HandleKeyDefinitionAt0(cursorTarget),
				1 => Enumerable.Empty<Location>(),
				2 => HandleKeyDefinitionAt2(cursorTarget),
				3 => Enumerable.Empty<Location>(),
				4 => HandleKeyDefinitionAt4(cursorTarget),
				_ => Enumerable.Empty<Location>()
			};
		}

		#region Private methods

		IEnumerable<Location> HandleKeyDefinitionAt0(CursorTarget cursorTarget)
		{
			var imageDefinitions = modSymbols.SpriteSequenceImageDefinitions.FirstOrDefault(x => x.Key == cursorTarget.TargetString);
			if (imageDefinitions != null)
				return imageDefinitions.Select(x => x.Location.ToLspLocation(imageDefinitions.Key.Length));

			return Enumerable.Empty<Location>();
		}

		IEnumerable<Location> HandleKeyDefinitionAt2(CursorTarget cursorTarget)
		{
			return HandleSpriteSequencePropertyKeyDefinition(cursorTarget);
		}

		IEnumerable<Location> HandleKeyDefinitionAt4(CursorTarget cursorTarget)
		{
			if (cursorTarget.TargetNode.ParentNode.ParentNode.Key == "Combine")
				return HandleSpriteSequencePropertyKeyDefinition(cursorTarget);

			return Enumerable.Empty<Location>();
		}

		IEnumerable<Location> HandleSpriteSequencePropertyKeyDefinition(CursorTarget cursorTarget)
		{
			var spriteSequenceFormat = symbolCache[cursorTarget.ModId].ModManifest.SpriteSequenceFormat.Type;
			var spriteSequenceType = symbolCache[cursorTarget.ModId].CodeSymbols.SpriteSequenceInfos[spriteSequenceFormat].First();

			var fieldInfo = spriteSequenceType.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.Key);
			if (fieldInfo.Name != null)
				return new[] { fieldInfo.Location.ToLspLocation(cursorTarget.TargetString.Length) };

			return Enumerable.Empty<Location>();
		}

		#endregion
	}
}
