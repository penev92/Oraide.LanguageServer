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
					TextDocumentSync = new TextDocumentSyncOptions
					{
						OpenClose = true,
						Change = TextDocumentSyncKind.Incremental,
						Save = new SaveOptions
						{
							IncludeText = true
						}
					},
					CompletionProvider = null,
					HoverProvider = true,
					SignatureHelpProvider = null,
					DefinitionProvider = true,
					TypeDefinitionProvider = false,
					ImplementationProvider = false,
					ReferencesProvider = false,
					DocumentHighlightProvider = false,
					DocumentSymbolProvider = false,
					CodeLensProvider = null,
					DocumentLinkProvider = null,
					DocumentFormattingProvider = false,
					DocumentRangeFormattingProvider = false,
					RenameProvider = false,
					FoldingRangeProvider = false,
					ExecuteCommandProvider = null,
					WorkspaceSymbolProvider = false,
					SemanticTokensProvider = new SemanticTokensOptions
					{
						Full = true,
						Range = false,
						Legend = new SemanticTokensLegend
						{
							tokenTypes = new[]
							{
								"class",
								"variable",
								"enum",
								"comment",
								"string",
								"keyword",
							},
							tokenModifiers = new[]
							{
								"declaration",
								"documentation",
							}
						}
					},

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
