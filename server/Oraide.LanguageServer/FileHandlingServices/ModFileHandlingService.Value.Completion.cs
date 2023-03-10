using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Extensions;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class ModFileHandlingService
	{
		protected override IEnumerable<CompletionItem> ValueCompletion(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetNodeIndentation switch
			{
				0 => HandleValueCompletionAt0(cursorTarget),
				_ => Enumerable.Empty<CompletionItem>()
			};
		}

		#region Private methods

		IEnumerable<CompletionItem> HandleValueCompletionAt0(CursorTarget cursorTarget)
		{
			var presentValues = cursorTarget.TargetNode.Value?.Split(',').Select(x => x.Trim()) ?? Enumerable.Empty<string>();

			// Asset loaders:
			if (cursorTarget.TargetNode.Key == "PackageFormats")
				return codeSymbols.AssetLoaders["Package"].Values
					.Where(x => !presentValues.Contains(x.Name))
					.Select(x => x.ToCompletionItem("Package loader"));

			if (cursorTarget.TargetNode.Key == "SoundFormats")
				return codeSymbols.AssetLoaders["Sound"].Values
					.Where(x => !presentValues.Contains(x.Name))
					.Select(x => x.ToCompletionItem("Sound loader"));

			if (cursorTarget.TargetNode.Key == "SpriteFormats")
				return codeSymbols.AssetLoaders["Sprite"].Values
					.Where(x => !presentValues.Contains(x.Name))
					.Select(x => x.ToCompletionItem("Sprite loader"));

			if (cursorTarget.TargetNode.Key == "VideoFormats")
				return codeSymbols.AssetLoaders["Video"].Values
					.Where(x => !presentValues.Contains(x.Name))
					.Select(x => x.ToCompletionItem("Video loader"));

			// TODO: Not implemented yet:
			//if (cursorTarget.TargetNode.Key == "TerrainFormat")
			//	return IHoverService.HoverFromHoverInfo("The type of **terrain** loader to use.", range);

			//if (cursorTarget.TargetNode.Key == "SpriteSequenceFormat")
			//	return IHoverService.HoverFromHoverInfo("The type of **sprite sequence** loader to use.", range);

			//if (cursorTarget.TargetNode.Key == "ModelSequenceFormat")
			//	return IHoverService.HoverFromHoverInfo("The type of **model** loader to use.", range);

			return Enumerable.Empty<CompletionItem>();
		}

		#endregion
	}
}
