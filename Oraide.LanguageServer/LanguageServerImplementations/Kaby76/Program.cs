using System;
using System.Threading.Tasks;
using Oraide.LanguageServer.LanguageServerImplementations.Kaby76.Implementation;
using Server;

namespace Oraide.LanguageServer.LanguageServerImplementations.Kaby76
{
	public class Program
	{
		public static async Task MainAsync(string[] args)
		{
			var workspaceFolderPath = args[0];
			var defaultOpenRaFolderPath = args[1];

			var stdin = Console.OpenStandardInput();
			var stdout = Console.OpenStandardOutput();
			stdin = new Tee(stdin, new Dup("editor"), Tee.StreamOwnership.OwnNone);
			//stdout = new Tee(stdout, new Dup("server"), Tee.StreamOwnership.OwnNone);
			var languageServer = new LSPServer(stdout, stdin, workspaceFolderPath, defaultOpenRaFolderPath);
			await Task.Delay(-1);
		}
	}
}
