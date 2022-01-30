using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Oraide.LanguageServer.Abstractions;
using Oraide.LanguageServer.Extensions;

namespace Oraide.LanguageServer
{
	class Program
	{
		private static async Task Main(string[] args)
		{
			// Add argument validation, maybe parsing, or maybe even an overkill NuGet package for handling them.

			await using var serviceProvider = new ServiceCollection()
				.AddSymbolProviders(args[0], args[1])
				.AddCaches()
				.AddLanguageServer()
				.AddGeneralLspMessageHandlers()
				.AddTextDocumentLspMessageHandlers()
				.AddWorkspaceLspMessageHandlers()
				.BuildServiceProvider();

			await serviceProvider
				.GetService<ILanguageServer>()
				.RunAsync();
		}
	}
}
