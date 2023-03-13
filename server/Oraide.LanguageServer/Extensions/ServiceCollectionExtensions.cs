using Microsoft.Extensions.DependencyInjection;
using Oraide.Core.Entities.MiniYaml;
using Oraide.Csharp;
using Oraide.LanguageServer.Abstractions;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;
using Oraide.LanguageServer.Caching;
using Oraide.LanguageServer.FileHandlingServices;
using Oraide.LanguageServer.LanguageServerProtocolHandlers;
using Oraide.LanguageServer.LanguageServerProtocolHandlers.Configuration;
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
				.AddSingleton<SymbolCache>()
				.AddSingleton<OpenFileCache>();
		}

		public static IServiceCollection AddFileHandlingServices(this IServiceCollection serviceCollection)
		{
			return serviceCollection
				.AddSingleton<ModFileHandlingService>()
				.AddSingleton<MapFileHandlingService>()
				.AddSingleton<RulesFileHandlingService>()
				.AddSingleton<SpriteSequencesFileHandlingService>()
				.AddSingleton<WeaponsFileHandlingService>();
		}

		public static IServiceCollection AddFileHandlerConfiguration(this IServiceCollection serviceCollection)
		{
			return serviceCollection
				.AddSingleton(provider =>
					FileTypeHandlerConfiguration.CreateBuilder()
						.For(FileType.ModFile).Use<ModFileHandlingService>()
						.For(FileType.Rules).Use<RulesFileHandlingService>()
						.For(FileType.Weapons).Use<WeaponsFileHandlingService>()
						.For(FileType.SpriteSequences).Use<SpriteSequencesFileHandlingService>()
						.For(FileType.MapFile).Use<MapFileHandlingService>()
						.For(FileType.MapRules).Use<RulesFileHandlingService>()
						.For(FileType.MapWeapons).Use<WeaponsFileHandlingService>()
						.For(FileType.MapSpriteSequences).Use<SpriteSequencesFileHandlingService>()
						.Build(provider));
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
				.AddSingleton<IRpcMessageHandler, TextDocumentHoverHandler>()
				.AddSingleton<IRpcMessageHandler, TextDocumentSymbolRequestHandler>()
				.AddSingleton<IRpcMessageHandler, TextDocumentColorHandler>();
		}

		public static IServiceCollection AddWorkspaceLspMessageHandlers(this IServiceCollection serviceCollection)
		{
			return serviceCollection
				.AddSingleton<IRpcMessageHandler, WorkspaceSymbolRequestHandler>()
				.AddSingleton<IRpcMessageHandler, DidChangeWatchedFilesHandler>();
		}
	}
}
