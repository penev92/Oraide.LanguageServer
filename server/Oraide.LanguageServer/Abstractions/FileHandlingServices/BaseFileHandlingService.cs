using System;
using System.Collections.Generic;
using System.Linq;
using LspTypes;
using OpenRA.MiniYamlParser;
using Oraide.Core;
using Oraide.Core.Entities;
using Oraide.Core.Entities.Csharp;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Caching;
using Oraide.LanguageServer.Caching.Entities;
using Oraide.LanguageServer.Extensions;
using Range = LspTypes.Range;

namespace Oraide.LanguageServer.Abstractions.FileHandlingServices
{
	public abstract class BaseFileHandlingService : IHoverService, IDefinitionService, ICompletionService, IReferencesService
	{
		protected readonly SymbolCache symbolCache;
		protected readonly OpenFileCache openFileCache;

		protected Range range;
		protected ModSymbols modSymbols;
		protected CodeSymbols codeSymbols;

		protected BaseFileHandlingService(SymbolCache symbolCache, OpenFileCache openFileCache)
		{
			this.symbolCache = symbolCache;
			this.openFileCache = openFileCache;
		}

		#region Service interface implementations

		Hover IHoverService.HandleHover(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetType switch
			{
				"key" => KeyHover(cursorTarget),
				"value" => ValueHover(cursorTarget),
				_ => null
			};
		}

		IEnumerable<Location> IDefinitionService.HandleDefinition(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetType switch
			{
				"key" => KeyDefinition(cursorTarget),
				"value" => ValueDefinition(cursorTarget),
				_ => null
			};
		}

		IEnumerable<CompletionItem> ICompletionService.HandleCompletion(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetType switch
			{
				"key" => KeyCompletion(cursorTarget),
				"value" => ValueCompletion(cursorTarget),
				_ => null
			};
		}

		IEnumerable<Location> IReferencesService.HandleReferences(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetType switch
			{
				"key" => KeyReferences(cursorTarget),
				"value" => ValueReferences(cursorTarget),
				_ => null
			};
		}

		#endregion

		#region Virtual protected methods

		protected virtual void Initialize(CursorTarget cursorTarget)
		{
			range = cursorTarget.ToRange();
			modSymbols = symbolCache[cursorTarget.ModId].ModSymbols;
			codeSymbols = symbolCache[cursorTarget.ModId].CodeSymbols;
		}

		protected abstract Hover KeyHover(CursorTarget cursorTarget);
		protected abstract Hover ValueHover(CursorTarget cursorTarget);

		protected abstract IEnumerable<Location> KeyDefinition(CursorTarget cursorTarget);
		protected abstract IEnumerable<Location> ValueDefinition(CursorTarget cursorTarget);

		protected abstract IEnumerable<CompletionItem> KeyCompletion(CursorTarget cursorTarget);
		protected abstract IEnumerable<CompletionItem> ValueCompletion(CursorTarget cursorTarget);

		protected abstract IEnumerable<Location> KeyReferences(CursorTarget cursorTarget);
		protected abstract IEnumerable<Location> ValueReferences(CursorTarget cursorTarget);

		#endregion

		#region Implemented protected methods

		protected bool TryMergeYamlFiles(IEnumerable<string> filePaths, out List<MiniYamlNode> nodes)
		{
			// As long as the merging passes there's a good chance we really do have a node like the target one to be removed.
			// If the target node removal doesn't have a corresponding node addition (is invalid), MiniYaml loading will throw.
			try
			{
				nodes = OpenRA.MiniYamlParser.MiniYaml.Load(filePaths);
				return true;
			}
			catch (Exception)
			{
				nodes = null;
				return false;
			}
		}

		protected string ResolveSpriteSequenceImageName(CursorTarget cursorTarget, ClassFieldInfo fieldInfo, IEnumerable<string> files)
		{
			// Initializing the target image name as the obscure default OpenRA uses - the actor name.
			var imageName = cursorTarget.TargetNode.ParentNode.ParentNode.Key;

			// Resolve the actual name that we need to use.
			var sequenceAttribute = fieldInfo.OtherAttributes.FirstOrDefault(x => x.Name == "SequenceReference");
			var imageFieldName = sequenceAttribute.Value != null && sequenceAttribute.Value.Contains(',')
				? sequenceAttribute.Value.Substring(0, sequenceAttribute.Value.IndexOf(','))
				: sequenceAttribute.Value;

			var modData = symbolCache[cursorTarget.ModId];
			var resolvedFileList = files.Select(x => OpenRaFolderUtils.ResolveFilePath(x, (modData.ModId, modData.ModFolder)));
			if (TryMergeYamlFiles(resolvedFileList, out var nodes))
			{
				// Check for overriding image names on the trait in question.
				var actorNode = nodes.First(x => x.Key == cursorTarget.TargetNode.ParentNode.ParentNode.Key);
				var traitNode = actorNode.Value.Nodes.FirstOrDefault(x => x.Key == cursorTarget.TargetNode.ParentNode.Key);
				var imageNode = traitNode?.Value.Nodes.FirstOrDefault(x => x.Key == imageFieldName);
				if (imageNode?.Value.Value != null)
					imageName = imageNode.Value.Value;
			}

			return imageName;
		}

		protected IEnumerable<SpriteSequenceDefinition> GetSpriteSequencesForImage(string modId, string imageName, MapManifest? mapManifest)
		{
			var files = new List<string>(symbolCache[modId].ModManifest.SpriteSequences);
			if (mapManifest?.WeaponsFiles != null)
				files.AddRange(mapManifest?.SpriteSequenceFiles);

			var resolvedFileList = files.Select(x => OpenRaFolderUtils.ResolveFilePath(x, (modId, symbolCache[modId].ModFolder)));

			if (TryMergeYamlFiles(resolvedFileList, out var imageNodes))
			{
				var sequenceNodes = imageNodes.FirstOrDefault(x => x.Key == imageName)?.Value.Nodes;
				var sequences = sequenceNodes?
					.Where(x => x.Key != "Defaults")
					.Select(x => new SpriteSequenceDefinition(x.Key, x.ParentNode?.Key, x.Value.Value,
						new MemberLocation("", 0, 0)));

				return sequences ?? Enumerable.Empty<SpriteSequenceDefinition>();
			}

			return Enumerable.Empty<SpriteSequenceDefinition>();
		}

		#endregion
	}
}
