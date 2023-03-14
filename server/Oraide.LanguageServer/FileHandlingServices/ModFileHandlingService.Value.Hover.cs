using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.FileHandlingServices;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class ModFileHandlingService
	{
		protected override Hover ValueHover(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetNodeIndentation switch
			{
				0 => HandleValueHoverAt0(cursorTarget),
				_ => null
			};
		}

		#region Private methods

		Hover HandleValueHoverAt0(CursorTarget cursorTarget)
		{
			// Asset loaders:
			if (cursorTarget.TargetNode.Key == "PackageFormats" && codeSymbols.AssetLoaders["Package"].TryGetValue(cursorTarget.TargetString, out var packageLoader))
				return IHoverService.HoverFromHoverInfo($"Package loader **{packageLoader.NameWithTypeSuffix}**.", range);

			if (cursorTarget.TargetNode.Key == "SoundFormats" && codeSymbols.AssetLoaders["Sound"].TryGetValue(cursorTarget.TargetString, out var soundLoader))
				return IHoverService.HoverFromHoverInfo($"Sound loader **{soundLoader.NameWithTypeSuffix}**.", range);

			if (cursorTarget.TargetNode.Key == "SpriteFormats" && codeSymbols.AssetLoaders["Sprite"].TryGetValue(cursorTarget.TargetString, out var spriteLoader))
				return IHoverService.HoverFromHoverInfo($"Sprite loader **{spriteLoader.NameWithTypeSuffix}**.", range);

			if (cursorTarget.TargetNode.Key == "VideoFormats" && codeSymbols.AssetLoaders["Video"].TryGetValue(cursorTarget.TargetString, out var videoLoader))
				return IHoverService.HoverFromHoverInfo($"Video loader **{videoLoader.NameWithTypeSuffix}**.", range);

			// TODO: Other *Formats are not implemented yet:
			//if (cursorTarget.TargetNode.Key == "TerrainFormat")
			//	return IHoverService.HoverFromHoverInfo("The type of **terrain** loader to use.", range);

			// TODO: This needs to be using the `ISpriteSequenceLoader` implementations (which we don't yet have), not `ISpriteSequence` implementations and needs to handle their properties.
			if (cursorTarget.TargetNode.Key == "SpriteSequenceFormat" && codeSymbols.SpriteSequenceInfos.Contains(cursorTarget.TargetString))
				return IHoverService.HoverFromHoverInfo($"Sprite sequence loader **{cursorTarget.TargetString}Loader**.", range);

			//if (cursorTarget.TargetNode.Key == "ModelSequenceFormat")
			//	return IHoverService.HoverFromHoverInfo("The type of **model** loader to use.", range);

			return null;
		}

		#endregion
	}
}
