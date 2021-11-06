using System;
using System.IO;
using LspTypes;
using Oraide.Core.Entities.Csharp;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Caching;

namespace Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers
{
	public abstract class BaseRpcMessageHandler : IRpcMessageHandler
	{
		protected static readonly object LockObject = new object();

		protected readonly bool trace = true;
		protected readonly SymbolCache symbolCache;
		protected readonly OpenFileCache openFileCache;

		protected BaseRpcMessageHandler(SymbolCache symbolCache, OpenFileCache openFileCache)
		{
			this.symbolCache = symbolCache;
			this.openFileCache = openFileCache;
		}

		protected bool TryGetTargetNode(TextDocumentPositionParams request, out YamlNode targetNode, out string targetType, out string targetString)
		{
			var filePath = request.TextDocument.Uri;
			var targetLineIndex = (int)request.Position.Line;
			var targetCharacterIndex = (int)request.Position.Character;

			if (!openFileCache.ContainsFile(filePath))
			{
				targetNode = default;
				targetType = "";
				targetString = ""; // TODO: Change to enum?
				return false;
			}

			var (fileLines, fileNodes) = openFileCache[filePath];

			var targetLine = fileLines[targetLineIndex];
			var pre = targetLine.Substring(0, targetCharacterIndex);
			var post = targetLine.Substring(targetCharacterIndex);

			if ((string.IsNullOrWhiteSpace(pre) && (post[0] == '\t' || post[0] == ' '))
			    || string.IsNullOrWhiteSpace(post))
			{
				targetNode = default;
				targetType = "";
				targetString = ""; // TODO: Change to enum?
				return false;
			}

			targetNode = fileNodes[targetLineIndex];
			targetString = "";
			if (pre.Contains(':'))
			{
				targetType = "value";
				var startIndex = 0;
				var endIndex = 1;
				var hasReached = false;
				while (endIndex < targetNode.Value.Length)
				{
					if (endIndex == targetCharacterIndex - targetLine.LastIndexOf(targetNode.Value, StringComparison.InvariantCulture))
						hasReached = true;

					if (targetNode.Value[endIndex] == ',' || endIndex == targetNode.Value.Length - 1)
					{
						if (!hasReached)
							startIndex = endIndex;
						else
						{
							targetString = targetNode.Value.Substring(startIndex, endIndex - startIndex + 1).Trim(' ', '\t', ',');
							Console.Error.WriteLine(targetString);
							break;
						}
					}

					endIndex++;
				}
			}
			else
			{
				targetType = "key";
				targetString = targetNode.Key;
			}

			return true;
		}

		protected bool TryGetTraitInfo(string traitName, out TraitInfo traitInfo, bool addInfoSuffix = true)
		{
			var searchString = addInfoSuffix ? $"{traitName}Info" : traitName;
			if (symbolCache.TraitInfos.ContainsKey(searchString))
			{
				traitInfo = symbolCache.TraitInfos[searchString];
				return true;
			}

			traitInfo = default;
			return false;
		}
	}
}
