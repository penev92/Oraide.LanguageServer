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
			if (cursorTarget.TargetNode.Key == "PackageFormats" && codeSymbols.AssetLoaders["Package"].TryGetValue(cursorTarget.TargetString, out var packageLoader))
				return new [] { packageLoader.Location.ToLspLocation(packageLoader.NameWithTypeSuffix.Length) };

			if (cursorTarget.TargetNode.Key == "SoundFormats" && codeSymbols.AssetLoaders["Sound"].TryGetValue(cursorTarget.TargetString, out var soundLoader))
				return new [] { soundLoader.Location.ToLspLocation(soundLoader.NameWithTypeSuffix.Length) };

			if (cursorTarget.TargetNode.Key == "SpriteFormats" && codeSymbols.AssetLoaders["Sprite"].TryGetValue(cursorTarget.TargetString, out var spriteLoader))
				return new [] { spriteLoader.Location.ToLspLocation(spriteLoader.NameWithTypeSuffix.Length) };

			if (cursorTarget.TargetNode.Key == "VideoFormats" && codeSymbols.AssetLoaders["Video"].TryGetValue(cursorTarget.TargetString, out var videoLoader))
				return new [] { videoLoader.Location.ToLspLocation(videoLoader.NameWithTypeSuffix.Length) };

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
