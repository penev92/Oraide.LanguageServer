using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Oraide.LanguageServer.LanguageServerProtocolHandlers;
using Oraide.LanguageServer.LanguageServerProtocolHandlers.TextDocument;

namespace Oraide.LanguageServer
{
	class Program
	{
		private static async Task Main(string[] args)
		{
			// Add argument validation, maybe parsing, or maybe even an overkill NuGet package for handling them.

			await using var serviceProvider = new ServiceCollection()
				.AddSingleton<ILanguageServer, OpenRaLanguageServer>()
				.AddSingleton<IRpcMessageHandler, InitializeHandler>()
				.AddSingleton<IRpcMessageHandler, ShutdownHandler>()
				.AddSingleton<IRpcMessageHandler, TextDocumentDefinitionHandler>()
				.AddSingleton<IRpcMessageHandler, TextDocumentHoverHandler>()
				.BuildServiceProvider();

			await serviceProvider
				.GetService<ILanguageServer>()
				.RunAsync();
		}
	}
}
