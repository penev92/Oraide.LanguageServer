﻿using System;
using System.Linq;
using LspTypes;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;
using Oraide.LanguageServer.Caching;

namespace Oraide.LanguageServer.LanguageServerProtocolHandlers.TextDocument
{
	public class TextDocumentDidChangeHandler : BaseRpcMessageHandler
	{
		public TextDocumentDidChangeHandler(SymbolCache symbolCache, OpenFileCache openFileCache)
			: base(symbolCache, openFileCache) { }

		[OraideCustomJsonRpcMethodTag(Methods.TextDocumentDidChangeName)]
		public void DidChangeTextDocument(DidChangeTextDocumentParams request)
		{
			lock (LockObject)
			{
				try
				{
					if (trace)
					{
						Console.Error.WriteLine("<-- TextDocument-DidChange");
						Console.Error.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(request));
					}

					openFileCache.AddOrUpdateOpenFile(request.TextDocument.Uri, request.ContentChanges.Last().Text);
				}
				catch (Exception e)
				{
				}
			}
		}
	}
}
