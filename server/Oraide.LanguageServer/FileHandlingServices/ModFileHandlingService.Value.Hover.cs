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
				return IHoverService.HoverFromHoverInfo($"Package loader {packageLoader.InfoName}", range);

			if (cursorTarget.TargetNode.Key == "SoundFormats" && codeSymbols.AssetLoaders["Sound"].TryGetValue(cursorTarget.TargetString, out var soundLoader))
				return IHoverService.HoverFromHoverInfo($"Sound loader {soundLoader.InfoName}", range);

			if (cursorTarget.TargetNode.Key == "SpriteFormats" && codeSymbols.AssetLoaders["Sprite"].TryGetValue(cursorTarget.TargetString, out var spriteLoader))
				return IHoverService.HoverFromHoverInfo($"Sprite loader {spriteLoader.InfoName}", range);

			if (cursorTarget.TargetNode.Key == "VideoFormats" && codeSymbols.AssetLoaders["Video"].TryGetValue(cursorTarget.TargetString, out var videoLoader))
				return IHoverService.HoverFromHoverInfo($"Video loader {videoLoader.InfoName}", range);

			// TODO: Not implemented yet:
			//if (cursorTarget.TargetNode.Key == "TerrainFormat")
			//	return IHoverService.HoverFromHoverInfo("The type of **terrain** loader to use.", range);

			//if (cursorTarget.TargetNode.Key == "SpriteSequenceFormat")
			//	return IHoverService.HoverFromHoverInfo("The type of **sprite sequence** loader to use.", range);

			//if (cursorTarget.TargetNode.Key == "ModelSequenceFormat")
			//	return IHoverService.HoverFromHoverInfo("The type of **model** loader to use.", range);

			return null;
		}

		#endregion
	}
}
