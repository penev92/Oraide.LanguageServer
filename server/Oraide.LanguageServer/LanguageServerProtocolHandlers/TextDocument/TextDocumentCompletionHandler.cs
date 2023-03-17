using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LspTypes;
using Oraide.Core;
using Oraide.Core.Entities;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.FileHandlingServices;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers.Configuration;
using Oraide.LanguageServer.Caching;

namespace Oraide.LanguageServer.LanguageServerProtocolHandlers.TextDocument
{
	public class TextDocumentCompletionHandler : BaseRpcMessageHandler
	{
		public TextDocumentCompletionHandler(SymbolCache symbolCache, OpenFileCache openFileCache, IFileTypeHandlerConfiguration fileHandlerConfiguration)
			: base(symbolCache, openFileCache, fileHandlerConfiguration) { }

		[OraideCustomJsonRpcMethodTag(Methods.TextDocumentCompletionName)]
		public CompletionList CompletionTextDocument(CompletionParams completionParams)
		{
			lock (LockObject)
			{
				try
				{
					if (trace)
						Console.Error.WriteLine($"[{DateTime.Now:hh:mm:ss.fff}] TextDocument-Completion");

					var completionItems = HandlePositionalRequest(completionParams) as IEnumerable<CompletionItem>;
					return new CompletionList
					{
						IsIncomplete = false,
						Items = completionItems?.ToArray()
					};

				}
				catch (Exception e)
				{
					Console.Error.WriteLine("EXCEPTION!!!");
					Console.Error.WriteLine(e.ToString());
				}

				return null;
			}
		}

		protected override bool TryGetCursorTarget(TextDocumentPositionParams positionParams, out CursorTarget target)
		{
			// HACK HACK HACK!!!
			// For whatever reason we receive the file URI borked - looks to be encoded for JSON, but the deserialization doesn't fix it.
			// No idea if this is an issue with VSCode or the LSP library used as there are currently no clients for other text editors.
			var incomingFileUriString = OpenRaFolderUtils.NormalizeFileUriString(positionParams.TextDocument.Uri);

			TryGetModId(incomingFileUriString, out var modId);
			var fileUri = new Uri(incomingFileUriString);
			var targetLineIndex = (int)positionParams.Position.Line;
			var targetCharacterIndex = (int)positionParams.Position.Character;

			var filePath = fileUri.AbsolutePath;
			var fileName = filePath.Split($"mods/{modId}/")[1];
			var fileReference = $"{modId}|{fileName}";

			ModManifest modManifest;
			if (symbolCache.ModSymbols.ContainsKey(modId))
			{
				modManifest = symbolCache[modId].ModManifest;
			}
			else
			{
				if (!symbolCache.KnownMods.ContainsKey(modId))
				{
					target = default;
					return false;
				}

				// Hope **someone** references this file...
				var modData = symbolCache.ModSymbols.Values.FirstOrDefault(x => x.ModManifest.AllFileReferences.Contains(fileReference));
				modManifest = modData?.ModManifest;
				modId = modData?.ModId;
			}

			if (modManifest == null)
			{
				target = default;
				return false;
			}

			// Determine file type.
			var fileType = FileType.Unknown;
			if (fileName == "mod.yaml")
				fileType = FileType.ModFile;
			else if (modManifest.RulesFiles.Contains(fileReference))
				fileType = FileType.Rules;
			else if (modManifest.WeaponsFiles.Contains(fileReference))
				fileType = FileType.Weapons;
			else if (modManifest.CursorsFiles.Contains(fileReference))
				fileType = FileType.Cursors;
			else if (modManifest.SpriteSequences.Contains(fileReference))
				fileType = FileType.SpriteSequences;
			else if (modManifest.ChromeLayout.Contains(fileReference))
				fileType = FileType.ChromeLayout;
			else if (Path.GetFileName(filePath) == "map.yaml" && symbolCache[modId].Maps.Any(x => x.MapFolder == Path.GetDirectoryName(filePath)))
				fileType = FileType.MapFile;
			else if (symbolCache[modId].Maps.Any(x => x.RulesFiles.Contains(fileReference)))
				fileType = FileType.MapRules;
			else if (symbolCache[modId].Maps.Any(x => x.WeaponsFiles.Contains(fileReference)))
				fileType = FileType.MapWeapons;
			else if (symbolCache[modId].Maps.Any(x => x.SpriteSequenceFiles.Contains(fileReference)))
				fileType = FileType.MapSpriteSequences;

			if (!openFileCache.ContainsFile(fileUri.AbsoluteUri))
			{
				target = default;
				return false;
			}

			var (fileNodes, flattenedNodes, fileLines) = openFileCache[fileUri.AbsoluteUri];

			var targetLine = fileLines[targetLineIndex];

			// If the target line is a comment we probably don't care about it - bail out early.
			if (Regex.IsMatch(targetLine, "^\\s#"))
			{
				target = default;
				return false;
			}

			var pre = targetLine.Substring(0, targetCharacterIndex);

			var targetNode = flattenedNodes[targetLineIndex];

			string sourceString;
			string targetType;

			if (pre.Contains(':'))
			{
				targetType = "value";
				sourceString = targetNode.Value;
			}
			else
			{
				if (pre.Contains('@'))
				{
					targetType = "keyIdentifier";
					sourceString = string.Empty;
				}
				else
				{
					targetType = "key";
					sourceString = targetNode.Key ?? string.Empty;
				}
			}

			TryGetTargetStringIndentation(targetNode, out var indentation);
			target = new CursorTarget(modId, fileType, fileReference, targetNode, targetType, sourceString,
				new MemberLocation(fileUri, targetLineIndex, targetCharacterIndex),
				new MemberLocation(fileUri, targetLineIndex, targetCharacterIndex), indentation);

			return true;
		}

		protected override IEnumerable<CompletionItem> HandlePositionalRequestInner(CursorTarget cursorTarget)
		{
			var service = fileHandlerConfiguration.GetService<ICompletionService>(cursorTarget);
			(service as BaseFileHandlingService)?.Initialize(cursorTarget);
			return service?.HandleCompletion(cursorTarget);
		}
	}
}
