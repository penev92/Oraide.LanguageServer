using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using LspTypes;
using Newtonsoft.Json.Linq;
using Oraide.LanguageServer.CodeParsers;
using Oraide.LanguageServer.YamlParsers;
using StreamJsonRpc;

namespace Oraide.LanguageServer.LanguageServerImplementations.Kaby76.Implementation
{
	/// <summary>
	/// This LanguageServer implementation is based on the sample from https://github.com/kaby76/lsp-types/blob/57e88a7cc6ce63a98040397be7281aa5689ada12/Sample/Server/Program.cs
	/// As well as inspired by https://github.com/kaby76/AntlrVSIX/blob/c4b6ba2f75736c65785b64c42efef91418286d2f/Server/LanguageServerTarget.cs
	/// </summary>
	public partial class LSPServer : INotifyPropertyChanged, IDisposable
	{
		private readonly JsonRpc rpc;
		private readonly ManualResetEvent disconnectEvent = new ManualResetEvent(false);
		private Dictionary<string, DiagnosticSeverity> diagnostics;
		public event PropertyChangedEventHandler PropertyChanged;
		public event EventHandler Disconnected;
		private bool isDisposed;
		private readonly Dictionary<string, Dictionary<string, ActorDefinition>> actorDefinitionsPerFile;
		private readonly IDictionary<string, TraitInfo> traitInfos;

		public LSPServer(Stream sender, Stream reader)
		{
			const string oraFolderPath = @"d:\Work.Personal\OpenRA\OpenRA";
			var actorDefinitions = ManualYamlParser.ParseRules(Path.Combine(oraFolderPath, @"mods\d2k\rules"));
			actorDefinitionsPerFile = actorDefinitions.GroupBy(x => x.Value.Location.FilePath)
				.ToDictionary(x => x.Key.Replace('\\', '/'), y => y.ToDictionary(k => k.Value.Name, l => l.Value));

			traitInfos = RoslynCodeParser.Parse(oraFolderPath);


			rpc = JsonRpc.Attach(sender, reader, this);
			rpc.Disconnected += OnRpcDisconnected;
		}

		private void OnRpcDisconnected(object sender, JsonRpcDisconnectedEventArgs e)
		{
			Exit();
		}

		private void NotifyPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (isDisposed) return;
			if (disposing)
			{
				// free managed resources
				disconnectEvent.Dispose();
			}

			isDisposed = true;
		}

		public void WaitForExit()
		{
			disconnectEvent.WaitOne();
		}

		~LSPServer()
		{
			// Finalizer calls Dispose(false)
			Dispose(false);
		}

		public void Exit()
		{
			disconnectEvent.Set();
			Disconnected?.Invoke(this, new EventArgs());
			System.Environment.Exit(0);
		}

		private static readonly object _object = new object();
		private readonly bool trace = true;

		[JsonRpcMethod(Methods.InitializeName)]
		public object Initialize(JToken arg)
		{
			lock (_object)
			{
				if (trace)
				{
					Console.Error.WriteLine("<-- Initialize");
					//Console.Error.WriteLine(arg.ToString());
				}

				var init_params = arg.ToObject<InitializeParams>();

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

				InitializeResult result = new InitializeResult
				{
					Capabilities = capabilities
				};
				string json = Newtonsoft.Json.JsonConvert.SerializeObject(result);
				if (trace)
				{
					// System.Console.Error.WriteLine("--> " + json);
				}

				return result;
			}
		}

		[JsonRpcMethod(Methods.InitializedName)]
		public void InitializedName(JToken arg)
		{
			lock (_object)
			{
				try
				{
					if (trace)
					{
						System.Console.Error.WriteLine("<-- Initialized");
						System.Console.Error.WriteLine(arg.ToString());
					}
				}
				catch (Exception)
				{
				}
			}
		}

		[JsonRpcMethod(Methods.ShutdownName)]
		public JToken ShutdownName()
		{
			lock (_object)
			{
				try
				{
					if (trace)
					{
						System.Console.Error.WriteLine("<-- Shutdown");
					}
				}
				catch (Exception)
				{
				}

				return null;
			}
		}

		[JsonRpcMethod(Methods.ExitName)]
		public void ExitName()
		{
			lock (_object)
			{
				try
				{
					if (trace)
					{
						System.Console.Error.WriteLine("<-- Exit");
					}

					Exit();
				}
				catch (Exception)
				{
				}
			}
		}
	}
}
