using System;
using LspTypes;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;

namespace Oraide.LanguageServer.LanguageServerProtocolHandlers
{
	public class InitializeHandler : BaseRpcMessageHandler
	{
		public InitializeHandler()
			: base(null, null) { }

		[OraideCustomJsonRpcMethodTag(Methods.InitializeName)]
		public InitializeResult Initialize(InitializeParams initializeParams)
		{
			lock (LockObject)
			{
				if (trace)
					Console.Error.WriteLine("<-- Initialize");

				var capabilities = new ServerCapabilities
				{
					TextDocumentSync = new TextDocumentSyncOptions // Defines how text documents are synced.
					{
						OpenClose = true,
						Change = TextDocumentSyncKind.Full,
						Save = null
					},
					CompletionProvider = new CompletionOptions // This enables basic completion with any extras disabled.
					{
						TriggerCharacters = null,
						AllCommitCharacters = null,
						ResolveProvider = false
					},
					HoverProvider = true, // The server provides hover support.
					SignatureHelpProvider = null,

					DeclarationProvider = true, // The server provides go to declaration support.
					DefinitionProvider = true, // The server provides goto definition support.
					TypeDefinitionProvider = true, // The server provides goto type definition support.
					ImplementationProvider = true, // The server provides goto implementation support.
					ReferencesProvider = true, // The server provides find references support.

					DocumentHighlightProvider = false,
					DocumentSymbolProvider = true,
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
						Commands = Array.Empty<string>()
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
					WorkspaceSymbolProvider = true, // The server provides workspace symbols.
					Workspace = null,
					Experimental = null
				};

				var result = new InitializeResult
				{
					Capabilities = capabilities
				};

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
						Console.Error.WriteLine("<-- Initialized");
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
