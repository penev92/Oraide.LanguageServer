using System;
using LspTypes;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;
using Oraide.LanguageServer.Caching;

namespace Oraide.LanguageServer.LanguageServerProtocolHandlers.TextDocument
{
	// This event is only triggered when a file is opened or a tab is switched to for the first time. Subsequent switching to an already opened tab will not trigger it.
	public class TextDocumentDidOpenHandler : BaseRpcMessageHandler
	{
		public TextDocumentDidOpenHandler(SymbolCache symbolCache, OpenFileCache openFileCache)
			: base(symbolCache, openFileCache) { }

		[OraideCustomJsonRpcMethodTag(Methods.TextDocumentDidOpenName)]
		public void DidOpenTextDocument(DidOpenTextDocumentParams request)
		{
			lock (LockObject)
			{
				try
				{
					if (trace)
						Console.Error.WriteLine("<-- TextDocument-DidOpen");

					openFileCache.AddOrUpdateOpenFile(request.TextDocument.Uri, request.TextDocument.Text);
				}
				catch (Exception e)
				{
					Console.Error.WriteLine("EXCEPTION!!!");
					Console.Error.WriteLine(e.ToString());
				}
			}
		}
	}
}
