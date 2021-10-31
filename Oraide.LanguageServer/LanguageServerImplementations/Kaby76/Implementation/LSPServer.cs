using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using LspTypes;
using Newtonsoft.Json.Linq;
using Oraide.Core.Entities.Csharp;
using Oraide.Core.Entities.MiniYaml;
using Oraide.Csharp;
using Oraide.MiniYaml;
using Oraide.MiniYaml.YamlParsers;
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

		// With the addition of other collections dedicated to holding definitions and this being delegated to a lookup table for client cursor position,
		// this is now redundant and should be replaced with on-demand parsing of the current file or with a cache that handles didOpen/didChange/didSave.
		// No point in loading everything up-front and also it could get stale really fast.
		private readonly IReadOnlyDictionary<string, ReadOnlyCollection<OpenRA.MiniYamlParser.MiniYamlNode>> parsedRulesPerFile;

		// TODO: Change to ILookup (to match `actorDefinitions` and also there may be more than one trait with the same name across namespaces).
		private readonly IDictionary<string, TraitInfo> traitInfos;

		/// <summary>
		/// A collection of all actor definitions in YAML (including abstract ones) grouped by they key/name.
		/// </summary>
		private readonly ILookup<string, ActorDefinition> actorDefinitions;

		public LSPServer(Stream sender, Stream reader, string workspaceFolderPath, string defaultOpenRaFolderPath)
		{
			Console.Error.WriteLine("WORKSPACE FOLDER PATH:");
			Console.Error.WriteLine(workspaceFolderPath);
			Console.Error.WriteLine("OPENRA FOLDER PATH:");
			Console.Error.WriteLine(defaultOpenRaFolderPath);

			var codeInformationProvider = new CodeInformationProvider(workspaceFolderPath, defaultOpenRaFolderPath);
			traitInfos = codeInformationProvider.GetTraitInfos();

			var yamlInformationProvider = new YamlInformationProvider(workspaceFolderPath);
			actorDefinitions = yamlInformationProvider.GetActorDefinitions();

			const string oraFolderPath = @"d:\Work.Personal\OpenRA\OpenRA";
			parsedRulesPerFile = OpenRAMiniYamlParser.GetParsedRulesPerFile(Path.Combine(oraFolderPath, @"mods"))
				.ToDictionary(x => x.Key.Replace('\\', '/'), y => y.Value);

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

		private bool TryGetTargetNode(TextDocumentPositionParams request, out OpenRA.MiniYamlParser.MiniYamlNode targetNode)
		{
			var position = request.Position;
			var targetLine = (int)position.Line;
			var targetCharacter = (int)position.Character;
			// var index = new LanguageServer.Module().GetIndex(line, character, document);
			var fileIdentifier = new Uri(Uri.UnescapeDataString(request.TextDocument.Uri)).AbsolutePath;

			if (parsedRulesPerFile.ContainsKey(fileIdentifier) && parsedRulesPerFile[fileIdentifier].Count >= targetLine)
			{
				targetNode = parsedRulesPerFile[fileIdentifier][targetLine];
				return true;
			}

			targetNode = default;
			return false;
		}

		private bool TryGetTraitInfo(string traitName, out TraitInfo traitInfo)
		{
			if (traitInfos.ContainsKey($"{traitName}Info"))
			{
				traitInfo = traitInfos[$"{traitName}Info"];
				return true;
			}

			traitInfo = default;
			return false;
		}
	}
}
