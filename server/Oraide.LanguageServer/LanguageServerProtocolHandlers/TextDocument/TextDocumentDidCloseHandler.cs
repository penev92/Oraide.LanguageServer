using System;
using LspTypes;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;
using Oraide.LanguageServer.Caching;

namespace Oraide.LanguageServer.LanguageServerProtocolHandlers.TextDocument
{
	// This event is only triggered when a file is opened or a tab is switched to for the first time. Subsequent switching to an already opened tab will not trigger it.
	public class TextDocumentDidCloseHandler : BaseRpcMessageHandler
	{
		public TextDocumentDidCloseHandler(SymbolCache symbolCache, OpenFileCache openFileCache)
			: base(symbolCache, openFileCache) { }

		[OraideCustomJsonRpcMethodTag(Methods.TextDocumentDidCloseName)]
		public void DidCloseTextDocument(DidCloseTextDocumentParams request)
		{
			lock (LockObject)
			{
				try
				{
					if (trace)
						Console.Error.WriteLine("<-- TextDocument-DidClose");

					openFileCache.RemoveOpenFile(request.TextDocument.Uri);
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
