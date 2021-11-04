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
			lock (_object)
			{
				if (trace)
				{
					Console.Error.WriteLine("<-- Initialize");
					Console.Error.WriteLine("<-- AAAAAAAAAAAAAAAAAAAAAAAAAAAAAASDASDASDASDASd");
					//Console.Error.WriteLine(arg.ToString());
				}

				// var init_params = arg.ToObject<InitializeParams>();

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
					FoldingRangeProvider =
						new SumType<bool, FoldingRangeOptions, FoldingRangeRegistrationOptions>(false),
					ExecuteCommandProvider = null,
					WorkspaceSymbolProvider = false,
					SemanticTokensProvider = new SemanticTokensOptions()
					{
						Full = true,
						Range = false,
						Legend = new SemanticTokensLegend()
						{
							tokenTypes = new string[]
							{
								"class",
								"variable",
								"enum",
								"comment",
								"string",
								"keyword",
							},
							tokenModifiers = new string[]
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
			lock (_object)
			{
				try
				{
					// var init_params = arg.ToObject<InitializedParams>();

					if (trace)
					{
						System.Console.Error.WriteLine("<-- Initialized");
						// System.Console.Error.WriteLine(arg.ToString());
					}
				}
				catch (Exception)
				{
				}
			}
		}
	}
}
