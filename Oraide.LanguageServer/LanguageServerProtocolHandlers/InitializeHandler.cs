using System;
using LspTypes;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;

namespace Oraide.LanguageServer.LanguageServerProtocolHandlers
{
	public class InitializeHandler : BaseRpcMessageHandler
	{
		public InitializeHandler(SymbolCache symbolCache)
			: base(symbolCache) { }

		[OraideCustomJsonRpcMethodTag(Methods.InitializeName)]
		public InitializeResult Initialize(InitializeParams initializeParams)
		{
			lock (LockObject)
			{
				if (trace)
				{
					Console.Error.WriteLine("<-- Initialize");
					Console.Error.WriteLine("<-- AAAAAAAAAAAAAAAAAAAAAAAAAAAAAASDASDASDASDASd");
					//Console.Error.WriteLine(arg.ToString());
				}

				var capabilities = new ServerCapabilities
				{
					TextDocumentSync = new TextDocumentSyncOptions // Defines how text documents are synced.
					{
						OpenClose = true,
						Change = TextDocumentSyncKind.Full,
						Save = null
					},
					CompletionProvider = null, // The server provides completion support.
					HoverProvider = true, // The server provides hover support.
					SignatureHelpProvider = null,

					DeclarationProvider = true, // The server provides go to declaration support.
					DefinitionProvider = true, // The server provides goto definition support.
					TypeDefinitionProvider = true, // The server provides goto type definition support.
					ImplementationProvider = true, // The server provides goto implementation support.
					ReferencesProvider = true, // The server provides find references support.

					DocumentHighlightProvider = false,
					DocumentSymbolProvider = false,
					CodeActionProvider = false,
					CodeLensProvider = null,

					DocumentLinkProvider = null,
					ColorProvider = false,
					DocumentFormattingProvider = false,
					DocumentRangeFormattingProvider = false,
					DocumentOnTypeFormattingProvider = null,
					RenameProvider = false,
					FoldingRangeProvider = false,
					ExecuteCommandProvider = new ExecuteCommandOptions
					{
						Commands = new string[0]
					},
					SelectionRangeProvider = false,
					LinkedEditingRangeProvider = false,
					CallHierarchyProvider = false,
					SemanticTokensProvider = new SemanticTokensOptions
					{
						Full = false,
						Range = false
					},
					MonikerProvider = false,
					WorkspaceSymbolProvider = false,
					Workspace = null,
					Experimental = null
				};

				var result = new InitializeResult
				{
					Capabilities = capabilities
				};

				// var json = Newtonsoft.Json.JsonConvert.SerializeObject(result);
				// if (trace)
				// {
				// 	// System.Console.Error.WriteLine("--> " + json);
				// }

				return result;
			}
		}

		[OraideCustomJsonRpcMethodTag(Methods.InitializedName)]
		public void InitializedName(InitializedParams initializedParams)
		{
			lock (LockObject)
			{
				try
				{
					if (trace)
					{
						Console.Error.WriteLine("<-- Initialized");
					}
				}
				catch (Exception)
				{
				}
			}
		}
	}
}
