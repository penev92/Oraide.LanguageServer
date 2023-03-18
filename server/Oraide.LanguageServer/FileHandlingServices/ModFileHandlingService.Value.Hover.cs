using System.Linq;
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
			if (cursorTarget.TargetNode.Key == "PackageFormats")
			{
				var loaders = codeSymbols.AssetLoaders["Package"][cursorTarget.TargetString];
				if (loaders.Any())
					return IHoverService.HoverFromHoverInfo($"Package loader **{loaders.First().NameWithTypeSuffix}**.", range);
			}

			if (cursorTarget.TargetNode.Key == "SoundFormats")
			{
				var loaders = codeSymbols.AssetLoaders["Sound"][cursorTarget.TargetString];
				if (loaders.Any())
					return IHoverService.HoverFromHoverInfo($"Sound loader **{loaders.First().NameWithTypeSuffix}**.", range);
			}

			if (cursorTarget.TargetNode.Key == "SpriteFormats")
			{
				var loaders = codeSymbols.AssetLoaders["Sprite"][cursorTarget.TargetString];
				if (loaders.Any())
					return IHoverService.HoverFromHoverInfo($"Sprite loader **{loaders.First().NameWithTypeSuffix}**.", range);
			}

			if (cursorTarget.TargetNode.Key == "VideoFormats")
			{
				var loaders = codeSymbols.AssetLoaders["Video"][cursorTarget.TargetString];
				if (loaders.Any())
					return IHoverService.HoverFromHoverInfo($"Video loader **{loaders.First().NameWithTypeSuffix}**.", range);
			}

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
