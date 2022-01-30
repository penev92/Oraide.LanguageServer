using Microsoft.Extensions.DependencyInjection;
using Oraide.Csharp;
using Oraide.LanguageServer.Abstractions;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;
using Oraide.LanguageServer.Caching;
using Oraide.LanguageServer.LanguageServerProtocolHandlers;
using Oraide.LanguageServer.LanguageServerProtocolHandlers.TextDocument;
using Oraide.LanguageServer.LanguageServerProtocolHandlers.Workspace;
using Oraide.MiniYaml;

namespace Oraide.LanguageServer.Extensions
{
	static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddSymbolProviders(this IServiceCollection serviceCollection, string workspaceFolderPath, string defaultOpenRaFolderPath)
		{
			return serviceCollection
				.AddSingleton(new CodeInformationProvider(workspaceFolderPath, defaultOpenRaFolderPath))
				.AddSingleton(new YamlInformationProvider(workspaceFolderPath));
		}

		public static IServiceCollection AddCaches(this IServiceCollection serviceCollection)
		{
			return serviceCollection
				.AddSingleton<SymbolCache>() // Just until consumers start using the factory.
				.AddSingleton<SymbolCacheFactory>()
				.AddSingleton(provider =>
				{
					var factory = provider.GetRequiredService<SymbolCacheFactory>();
					return factory.CreateSymbolCachesPerMod();
				})
				.AddSingleton<OpenFileCache>();
		}

		public static IServiceCollection AddLanguageServer(this IServiceCollection serviceCollection)
		{
			return serviceCollection.AddSingleton<ILanguageServer, OpenRaLanguageServer>();
		}

		public static IServiceCollection AddGeneralLspMessageHandlers(this IServiceCollection serviceCollection)
		{
			return serviceCollection
				.AddSingleton<IRpcMessageHandler, InitializeHandler>()
				.AddSingleton<IRpcMessageHandler, ShutdownHandler>();
		}

		public static IServiceCollection AddTextDocumentLspMessageHandlers(this IServiceCollection serviceCollection)
		{
			return serviceCollection
				.AddSingleton<IRpcMessageHandler, TextDocumentCompletionHandler>()
				.AddSingleton<IRpcMessageHandler, TextDocumentDidOpenHandler>()
				.AddSingleton<IRpcMessageHandler, TextDocumentDidChangeHandler>()
				.AddSingleton<IRpcMessageHandler, TextDocumentDidCloseHandler>()
				.AddSingleton<IRpcMessageHandler, TextDocumentDefinitionHandler>()
				.AddSingleton<IRpcMessageHandler, TextDocumentReferencesHandler>()
				.AddSingleton<IRpcMessageHandler, TextDocumentHoverHandler>();
		}

		public static IServiceCollection AddWorkspaceLspMessageHandlers(this IServiceCollection serviceCollection)
		{
			return serviceCollection
				.AddSingleton<IRpcMessageHandler, WorkspaceSymbolRequestHandler>()
				.AddSingleton<IRpcMessageHandler, DidChangeWatchedFilesHandler>();
		}
	}
}
