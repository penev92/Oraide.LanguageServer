using System;
using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;
using Oraide.LanguageServer.Caching;
using Oraide.LanguageServer.Extensions;

namespace Oraide.LanguageServer.LanguageServerProtocolHandlers.TextDocument
{
	public class TextDocumentSymbolRequestHandler : BaseRpcMessageHandler
	{
		public TextDocumentSymbolRequestHandler(SymbolCache symbolCache, OpenFileCache openFileCache)
			: base(symbolCache, openFileCache) { }

		[OraideCustomJsonRpcMethodTag(Methods.TextDocumentDocumentSymbolName)]
		public IEnumerable<DocumentSymbol> DocumentSymbols(DocumentSymbolParams request)
		{
			lock (LockObject)
			{
				try
				{
					if (trace)
						Console.Error.WriteLine("<-- TextDocument-DocumentSymbol");

					return openFileCache[request.TextDocument.Uri].YamlNodes.Where(x => x.Key != null).Select(ConvertNodeToDocumentSymbol);
				}
				catch (Exception e)
				{
					Console.Error.WriteLine("EXCEPTION!!!");
					Console.Error.WriteLine(e.ToString());
				}

				return Enumerable.Empty<DocumentSymbol>();
			}
		}

		DocumentSymbol ConvertNodeToDocumentSymbol(YamlNode yamlNode)
		{
			TryGetTargetStringIndentation(yamlNode, out var indentation);
			var symbolKind = indentation switch
			{
				0 => SymbolKind.Struct,
				1 => SymbolKind.Field,
				_ => SymbolKind.Key
			};

			var selectionRange = yamlNode.Location.ToRange(yamlNode.Key.Length + indentation + 9001); // "To the end of the line" or close.
			var lastHeirNode = GetLastHeirNode(yamlNode);
			var range = lastHeirNode == null
				? selectionRange
				: new LspTypes.Range
				{
					Start = new Position((uint)yamlNode.Location.LineNumber - 1, (uint)yamlNode.Location.CharacterPosition),
					End = new Position((uint)lastHeirNode.Location.LineNumber - 1, 9101) // "To the end of the line" or close. Must be bigger than selectionRange's.
				};

			return new DocumentSymbol
			{
				Name = yamlNode.Key,
				Kind = symbolKind,
				Range = range,
				SelectionRange = selectionRange,
				Children = yamlNode.ChildNodes?.Where(x => x.Key != null).Select(ConvertNodeToDocumentSymbol).ToArray()
			};
		}

		YamlNode GetLastHeirNode(YamlNode yamlNode)
		{
			if (yamlNode.ChildNodes is { Count: > 0 })
				return GetLastHeirNode(yamlNode.ChildNodes.MaxBy(x => x.Location.LineNumber));

			return yamlNode;
		}
	}
}
