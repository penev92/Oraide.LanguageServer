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

			// Using .First() is not great but we have no way to differentiate between traits of the same name
			// until the server learns the concept of a mod and loaded assemblies.
			if (cursorTarget.TargetNode.Key == "PackageFormats")
				return codeSymbols.AssetLoaders["Package"]
					.Where(x => !presentValues.Contains(x.Key))
					.Select(x => x.First().ToCompletionItem("Package loader"));

			if (cursorTarget.TargetNode.Key == "SoundFormats")
				return codeSymbols.AssetLoaders["Sound"]
					.Where(x => !presentValues.Contains(x.Key))
					.Select(x => x.First().ToCompletionItem("Sound loader"));

			if (cursorTarget.TargetNode.Key == "SpriteFormats")
				return codeSymbols.AssetLoaders["Sprite"]
					.Where(x => !presentValues.Contains(x.Key))
					.Select(x => x.First().ToCompletionItem("Sprite loader"));

			if (cursorTarget.TargetNode.Key == "VideoFormats")
				return codeSymbols.AssetLoaders["Video"]
					.Where(x => !presentValues.Contains(x.Key))
					.Select(x => x.First().ToCompletionItem("Video loader"));

			// TODO: Other *Formats are not implemented yet:
			//if (cursorTarget.TargetNode.Key == "TerrainFormat")
			//	return IHoverService.HoverFromHoverInfo("The type of **terrain** loader to use.", range);

			// TODO: This needs to be using the `ISpriteSequenceLoader` implementations (which we don't yet have), not `ISpriteSequence` implementations and needs to handle their properties.
			if (cursorTarget.TargetNode.Key == "SpriteSequenceFormat")
				return codeSymbols.SpriteSequenceInfos
					.Where(x => !presentValues.Contains(x.Key))
					.SelectMany(x => x.Select(y => y.ToCompletionItem("Sprite Sequence Loader")));

			//if (cursorTarget.TargetNode.Key == "ModelSequenceFormat")
			//	return IHoverService.HoverFromHoverInfo("The type of **model** loader to use.", range);

			return Enumerable.Empty<CompletionItem>();
		}

		#endregion
	}
}
