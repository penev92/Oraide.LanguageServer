using System;
using System.IO;
using LspTypes;
using Oraide.Core.Entities.Csharp;
using Oraide.Core.Entities.MiniYaml;

namespace Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers
{
	public abstract class BaseRpcMessageHandler : IRpcMessageHandler
	{
		protected static readonly object LockObject = new object();

		protected readonly bool trace = true;
		protected readonly SymbolCache symbolCache;

		protected BaseRpcMessageHandler(SymbolCache symbolCache)
		{
			this.symbolCache = symbolCache;
		}

		protected bool TryGetTargetNode(TextDocumentPositionParams request, out YamlNode targetNode, out string targetType, out string targetString)
		{
			var position = request.Position;
			var targetLine = (int)position.Line;
			var targetCharacter = (int)position.Character;
			// var index = new LanguageServer.Module().GetIndex(line, character, document);
			var fileIdentifier = new Uri(Uri.UnescapeDataString(request.TextDocument.Uri)).AbsolutePath;

			if (symbolCache.ParsedRulesPerFile.ContainsKey(fileIdentifier) && symbolCache.ParsedRulesPerFile[fileIdentifier].Count >= targetLine)
			{
				var file = new Uri(request.TextDocument.Uri).LocalPath.Substring(1);
				var lines = File.ReadAllLines(file);	// TODO: Cache these.
				var line = lines[request.Position.Line];
				var pre = line.Substring(0, (int)request.Position.Character);
				var post = line.Substring((int)request.Position.Character);

				if ((string.IsNullOrWhiteSpace(pre) && (post[0] == '\t' || post[0] == ' '))
				    || string.IsNullOrWhiteSpace(post))
				{
					targetNode = default;
					targetType = "";
					targetString = ""; // TODO: Change to enum?
					return false;
				}

				targetNode = symbolCache.ParsedRulesPerFile[fileIdentifier][targetLine];
				targetString = "";
				if (pre.Contains(':'))
				{
					targetType = "value";
					var startIndex = 0;
					var endIndex = 1;
					var hasReached = false;
					while (endIndex < targetNode.Value.Length)
					{
						if (endIndex == request.Position.Character - line.LastIndexOf(targetNode.Value, StringComparison.InvariantCulture))
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

			targetNode = default;
			targetType = "";
			targetString = ""; // TODO: Change to enum?
			return false;
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
