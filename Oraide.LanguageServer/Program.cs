using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Oraide.LanguageServer.CodeParsers;
using Oraide.LanguageServer.YamlParsers;

namespace Oraide.LanguageServer
{
	class Program
	{
		// private static void Main(string[] args) => TestParsers();
		private static void Main(string[] args) => TestLanguageServerAsync(args).Wait();

		private static async Task TestLanguageServerAsync(string[] args)
		{
			await LanguageServerImplementations.Kaby76.Program.MainAsync(args);
		}

		static void TestParsers()
		{
			const string oraFolderPath = @"d:\Work.Personal\OpenRA\OpenRA\";

			// ParseCode(oraFolderPath);
			ParseYaml(oraFolderPath);
		}

		static void ParseCode(in string oraFolderPath)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var traitDictionary = RoslynCodeParser.Parse(oraFolderPath);

			Console.WriteLine(stopwatch.Elapsed);
			Console.WriteLine(traitDictionary.Count);
		}

		static void ParseYaml(in string oraFolderPath)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			// ManualYamlParser.Parse(oraFolderPath);
			OpenRAMiniYamlParser.Parse(oraFolderPath);

			Console.WriteLine(stopwatch.Elapsed);
		}
	}
}
