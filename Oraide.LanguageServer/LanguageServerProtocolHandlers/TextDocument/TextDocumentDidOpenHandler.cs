using System;
using LspTypes;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;

namespace Oraide.LanguageServer.LanguageServerProtocolHandlers.TextDocument
{
	// This event is only triggered when a file is opened or a tab is switched to for the first time. Subsequent switching to an already opened tab will not trigger it.
	public class TextDocumentDidOpenHandler : BaseRpcMessageHandler
	{
		public TextDocumentDidOpenHandler(SymbolCache symbolCache)
			: base(symbolCache) { }

		[OraideCustomJsonRpcMethodTag(Methods.TextDocumentDidOpenName)]
		public void DidOpenTextDocument(DidOpenTextDocumentParams request)
		{
			lock (LockObject)
			{
				try
				{
					if (trace)
					{
						Console.Error.WriteLine("<-- TextDocument-DidOpen");
						Console.Error.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(request));
					}
				}
				catch (Exception e)
				{
				}
			}
		}
	}
}
