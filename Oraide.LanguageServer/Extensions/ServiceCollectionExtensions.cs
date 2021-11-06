using Microsoft.Extensions.DependencyInjection;
using Oraide.LanguageServer.Abstractions;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;
using Oraide.LanguageServer.Caching;
using Oraide.LanguageServer.LanguageServerProtocolHandlers;
using Oraide.LanguageServer.LanguageServerProtocolHandlers.TextDocument;
using Oraide.LanguageServer.LanguageServerProtocolHandlers.Workspace;

namespace Oraide.LanguageServer.Extensions
{
	static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddFileCaches(this IServiceCollection serviceCollection, string workspaceFolderPath, string defaultOpenRaFolderPath)
		{
			return serviceCollection
				.AddSingleton(provider => new SymbolCache(workspaceFolderPath, defaultOpenRaFolderPath))
				.AddSingleton(provider => new OpenFileCache());
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
				.AddSingleton<IRpcMessageHandler, TextDocumentDidOpenHandler>()
				.AddSingleton<IRpcMessageHandler, TextDocumentDidChangeHandler>()
				.AddSingleton<IRpcMessageHandler, TextDocumentDidCloseHandler>()
				.AddSingleton<IRpcMessageHandler, TextDocumentDefinitionHandler>()
				.AddSingleton<IRpcMessageHandler, TextDocumentHoverHandler>();
		}

		public static IServiceCollection AddWorkspaceLspMessageHandlers(this IServiceCollection serviceCollection)
		{
			return serviceCollection
				.AddSingleton<IRpcMessageHandler, DidChangeWatchedFilesHandler>();
		}
	}
}
