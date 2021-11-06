using System;
using LspTypes;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;

namespace Oraide.LanguageServer.LanguageServerProtocolHandlers.TextDocument
{
	public class TextDocumentDidChangeHandler : BaseRpcMessageHandler
	{
		public TextDocumentDidChangeHandler(SymbolCache symbolCache)
			: base(symbolCache) { }

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
				}
				catch (Exception e)
				{
				}
			}
		}
	}
}
