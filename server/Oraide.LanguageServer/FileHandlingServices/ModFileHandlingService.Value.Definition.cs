using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Extensions;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class ModFileHandlingService
	{
		protected override IEnumerable<Location> ValueDefinition(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetNodeIndentation switch
			{
				0 => HandleValueDefinitionAt0(cursorTarget),
				_ => Enumerable.Empty<Location>()
			};
		}

		#region Private methods

		IEnumerable<Location> HandleValueDefinitionAt0(CursorTarget cursorTarget)
		{
			// Asset loaders:
			if (cursorTarget.TargetNode.Key == "PackageFormats")
				return codeSymbols.AssetLoaders["Package"][cursorTarget.TargetString].Select(x => x.Location.ToLspLocation(x.NameWithTypeSuffix.Length));

			if (cursorTarget.TargetNode.Key == "SoundFormats")
				return codeSymbols.AssetLoaders["Sound"][cursorTarget.TargetString].Select(x => x.Location.ToLspLocation(x.NameWithTypeSuffix.Length));

			if (cursorTarget.TargetNode.Key == "SpriteFormats")
				return codeSymbols.AssetLoaders["Sprite"][cursorTarget.TargetString].Select(x => x.Location.ToLspLocation(x.NameWithTypeSuffix.Length));

			if (cursorTarget.TargetNode.Key == "VideoFormats")
				return codeSymbols.AssetLoaders["Video"][cursorTarget.TargetString].Select(x => x.Location.ToLspLocation(x.NameWithTypeSuffix.Length));

			// TODO: Other *Formats are not implemented yet:
			//if (cursorTarget.TargetNode.Key == "TerrainFormat")
			//	return IHoverService.HoverFromHoverInfo("The type of **terrain** loader to use.", range);

			// TODO: This needs to be using the `ISpriteSequenceLoader` implementations (which we don't yet have), not `ISpriteSequence` implementations and needs to handle their properties.
			if (cursorTarget.TargetNode.Key == "SpriteSequenceFormat" && codeSymbols.SpriteSequenceInfos.Contains(cursorTarget.TargetString))
				return new [] { codeSymbols.SpriteSequenceInfos[cursorTarget.TargetString].First().Location.ToLspLocation(cursorTarget.TargetString.Length) };

			//if (cursorTarget.TargetNode.Key == "ModelSequenceFormat")
			//	return IHoverService.HoverFromHoverInfo("The type of **model** loader to use.", range);

			return Enumerable.Empty<Location>();
		}

		#endregion
	}
}
