using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Oraide.LanguageServer.LanguageServerImplementations.Kaby76.Implementation;
using Oraide.LanguageServer.LanguageServerProtocolHandlers;

namespace Oraide.LanguageServer
{
	public partial class OpenRaLanguageServer : ILanguageServer
	{
		readonly IEnumerable<IRpcMessageHandler> messageHandlers;

		public OpenRaLanguageServer(IEnumerable<IRpcMessageHandler> messageHandlers)
		{
			this.messageHandlers = messageHandlers;
		}

		public async Task RunAsync()
		{
			var stdin = Console.OpenStandardInput();
			var stdout = Console.OpenStandardOutput();
			// stdin = new Tee(stdin, new Dup("editor"), Tee.StreamOwnership.OwnNone);
			// stdout = new Tee(stdout, new Dup("server"), Tee.StreamOwnership.OwnNone);
			var languageServer = new LSPServer(stdout, stdin, messageHandlers);
			await Task.Delay(-1);
		}
	}
}
