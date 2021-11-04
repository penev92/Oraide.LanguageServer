using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using LspTypes;
using Newtonsoft.Json.Linq;
using Oraide.Core.Entities.Csharp;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.LanguageServerProtocolHandlers;
using StreamJsonRpc;

namespace Oraide.LanguageServer.LanguageServerImplementations.Kaby76.Implementation
{
	/// <summary>
	/// This LanguageServer implementation is based on the sample from https://github.com/kaby76/lsp-types/blob/57e88a7cc6ce63a98040397be7281aa5689ada12/Sample/Server/Program.cs
	/// As well as inspired by https://github.com/kaby76/AntlrVSIX/blob/c4b6ba2f75736c65785b64c42efef91418286d2f/Server/LanguageServerTarget.cs
	/// </summary>
	public class LSPServer : INotifyPropertyChanged, IDisposable
	{
		private readonly JsonRpc rpc;
		private readonly ManualResetEvent disconnectEvent = new ManualResetEvent(false);
		private Dictionary<string, DiagnosticSeverity> diagnostics;
		public event PropertyChangedEventHandler PropertyChanged;
		public event EventHandler Disconnected;
		private bool isDisposed;

		private readonly IReadOnlyDictionary<string, (IRpcMessageHandler MessageHandlerClass, MethodInfo MethodInfo)> rpcMessageHandlers;

		public LSPServer(Stream sender, Stream reader, IEnumerable<IRpcMessageHandler> messageHandlers)
		{
			rpcMessageHandlers = messageHandlers
				.SelectMany(x => x.GetType().GetMethods()
					.Select(y => new KeyValuePair<string, (IRpcMessageHandler MessageHandlerClass, MethodInfo MethodInfo)>(y.CustomAttributes
						.FirstOrDefault(z => z.AttributeType.Name == nameof(OraideCustomJsonRpcMethodTagAttribute))?.ConstructorArguments.FirstOrDefault().Value.ToString(), (x, y)))
					.Where(y => y.Key != null))
				.ToDictionary(x => x.Key, y => y.Value);

			rpc = JsonRpc.Attach(sender, reader, this);
			rpc.Disconnected += OnRpcDisconnected;
		}

		[JsonRpcMethod(Methods.InitializeName)]
		public InitializeResult Initialize(JToken arg)
		{
			var (messageHandlerClass, methodInfo) = rpcMessageHandlers[Methods.InitializeName];
			return (InitializeResult)methodInfo.Invoke(messageHandlerClass, new object[] { arg.ToObject<InitializeParams>() });
		}

		[JsonRpcMethod(Methods.InitializedName)]
		public void InitializedName(JToken arg)
		{
			var (messageHandlerClass, methodInfo) = rpcMessageHandlers[Methods.InitializedName];
			methodInfo.Invoke(messageHandlerClass, new object[] { arg.ToObject<InitializedParams>() });
		}

		[JsonRpcMethod(Methods.ShutdownName)]
		public JToken ShutdownName()
		{
			var (messageHandlerClass, methodInfo) = rpcMessageHandlers[Methods.ShutdownName];
			return (JToken)methodInfo.Invoke(messageHandlerClass, null);
		}

		[JsonRpcMethod(Methods.ExitName)]
		public void ExitName()
		{
			var (messageHandlerClass, methodInfo) = rpcMessageHandlers[Methods.ExitName];
			methodInfo.Invoke(messageHandlerClass, null);
		}

		[JsonRpcMethod(Methods.TextDocumentDefinitionName)]
		public IEnumerable<Location> Definition(JToken arg)
		{
			var (messageHandlerClass, methodInfo) = rpcMessageHandlers[Methods.TextDocumentDefinitionName];
			return (IEnumerable<Location>)methodInfo.Invoke(messageHandlerClass, new object[] { arg.ToObject<TextDocumentPositionParams>() });
		}

		[JsonRpcMethod(Methods.TextDocumentHoverName)]
		public Hover Hover(JToken arg)
		{
			var (messageHandlerClass, methodInfo) = rpcMessageHandlers[Methods.TextDocumentHoverName];
			return (Hover)methodInfo.Invoke(messageHandlerClass, new object[] { arg.ToObject<TextDocumentPositionParams>() });
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

		// private static readonly object _object = new object();
		// private readonly bool trace = true;
		//
		// [JsonRpcMethod(Methods.InitializeName)]
		// public InitializeResult Initialize(JToken arg)
		// {
		// 	lock (_object)
		// 	{
		// 		if (trace)
		// 		{
		// 			Console.Error.WriteLine("<-- Initialize");
		// 			//Console.Error.WriteLine(arg.ToString());
		// 		}
		//
		// 		var init_params = arg.ToObject<InitializeParams>();
		//
		// 		var capabilities = new ServerCapabilities
		// 		{
		// 			TextDocumentSync = new TextDocumentSyncOptions
		// 			{
		// 				OpenClose = true,
		// 				Change = TextDocumentSyncKind.Incremental,
		// 				Save = new SaveOptions
		// 				{
		// 					IncludeText = true
		// 				}
		// 			},
		// 			CompletionProvider = null,
		// 			HoverProvider = true,
		// 			SignatureHelpProvider = null,
		// 			DefinitionProvider = true,
		// 			TypeDefinitionProvider = false,
		// 			ImplementationProvider = false,
		// 			ReferencesProvider = false,
		// 			DocumentHighlightProvider = false,
		// 			DocumentSymbolProvider = false,
		// 			CodeLensProvider = null,
		// 			DocumentLinkProvider = null,
		// 			DocumentFormattingProvider = false,
		// 			DocumentRangeFormattingProvider = false,
		// 			RenameProvider = false,
		// 			FoldingRangeProvider =
		// 				new SumType<bool, FoldingRangeOptions, FoldingRangeRegistrationOptions>(false),
		// 			ExecuteCommandProvider = null,
		// 			WorkspaceSymbolProvider = false,
		// 			SemanticTokensProvider = new SemanticTokensOptions()
		// 			{
		// 				Full = true,
		// 				Range = false,
		// 				Legend = new SemanticTokensLegend()
		// 				{
		// 					tokenTypes = new string[]
		// 					{
		// 						"class",
		// 						"variable",
		// 						"enum",
		// 						"comment",
		// 						"string",
		// 						"keyword",
		// 					},
		// 					tokenModifiers = new string[]
		// 					{
		// 						"declaration",
		// 						"documentation",
		// 					}
		// 				}
		// 			},
		//
		// 		};
		//
		// 		var result = new InitializeResult
		// 		{
		// 			Capabilities = capabilities
		// 		};
		//
		// 		// var json = Newtonsoft.Json.JsonConvert.SerializeObject(result);
		// 		// if (trace)
		// 		// {
		// 		// 	// System.Console.Error.WriteLine("--> " + json);
		// 		// }
		//
		// 		return result;
		// 	}
		// }
		//
		// [JsonRpcMethod(Methods.InitializedName)]
		// public void InitializedName(JToken arg)
		// {
		// 	lock (_object)
		// 	{
		// 		try
		// 		{
		// 			if (trace)
		// 			{
		// 				System.Console.Error.WriteLine("<-- Initialized");
		// 				// System.Console.Error.WriteLine(arg.ToString());
		// 			}
		// 		}
		// 		catch (Exception)
		// 		{
		// 		}
		// 	}
		// }
		//
		// [JsonRpcMethod(Methods.ShutdownName)]
		// public JToken ShutdownName()
		// {
		// 	lock (_object)
		// 	{
		// 		try
		// 		{
		// 			if (trace)
		// 			{
		// 				System.Console.Error.WriteLine("<-- Shutdown");
		// 			}
		// 		}
		// 		catch (Exception)
		// 		{
		// 		}
		//
		// 		return null;
		// 	}
		// }
		//
		// [JsonRpcMethod(Methods.ExitName)]
		// public void ExitName()
		// {
		// 	lock (_object)
		// 	{
		// 		try
		// 		{
		// 			if (trace)
		// 			{
		// 				System.Console.Error.WriteLine("<-- Exit");
		// 			}
		//
		// 			Exit();
		// 		}
		// 		catch (Exception)
		// 		{
		// 		}
		// 	}
		// }
		//
		// private bool TryGetTargetNode(TextDocumentPositionParams request, out YamlNode targetNode, out string targetType, out string targetString)
		// {
		// 	var position = request.Position;
		// 	var targetLine = (int)position.Line;
		// 	var targetCharacter = (int)position.Character;
		// 	// var index = new LanguageServer.Module().GetIndex(line, character, document);
		// 	var fileIdentifier = new Uri(Uri.UnescapeDataString(request.TextDocument.Uri)).AbsolutePath;
		//
		// 	// if (symbolCache.ParsedRulesPerFile.ContainsKey(fileIdentifier) && symbolCache.ParsedRulesPerFile[fileIdentifier].Count >= targetLine)
		// 	// {
		// 	// 	var file = new Uri(request.TextDocument.Uri).LocalPath.Substring(1);
		// 	// 	var lines = File.ReadAllLines(file);	// TODO: Cache these.
		// 	// 	var line = lines[request.Position.Line];
		// 	// 	var pre = line.Substring(0, (int)request.Position.Character);
		// 	// 	var post = line.Substring((int)request.Position.Character);
		// 	//
		// 	// 	if ((string.IsNullOrWhiteSpace(pre) && (post[0] == '\t' || post[0] == ' '))
		// 	// 	    || string.IsNullOrWhiteSpace(post))
		// 	// 	{
		// 	// 		targetNode = default;
		// 	// 		targetType = "";
		// 	// 		targetString = ""; // TODO: Change to enum?
		// 	// 		return false;
		// 	// 	}
		// 	//
		// 	// 	targetNode = symbolCache.ParsedRulesPerFile[fileIdentifier][targetLine];
		// 	// 	targetString = "";
		// 	// 	if (pre.Contains(':'))
		// 	// 	{
		// 	// 		targetType = "value";
		// 	// 		var startIndex = 0;
		// 	// 		var endIndex = 1;
		// 	// 		var hasReached = false;
		// 	// 		while (endIndex < targetNode.Value.Length)
		// 	// 		{
		// 	// 			if (endIndex == request.Position.Character - line.LastIndexOf(targetNode.Value))
		// 	// 				hasReached = true;
		// 	//
		// 	// 			if (targetNode.Value[endIndex] == ',' || endIndex == targetNode.Value.Length - 1)
		// 	// 			{
		// 	// 				if (!hasReached)
		// 	// 					startIndex = endIndex;
		// 	// 				else
		// 	// 				{
		// 	// 					targetString = targetNode.Value.Substring(startIndex, endIndex - startIndex + 1).Trim(' ', '\t', ',');
		// 	// 					Console.Error.WriteLine(targetString);
		// 	// 					break;
		// 	// 				}
		// 	// 			}
		// 	//
		// 	// 			endIndex++;
		// 	// 		}
		// 	// 	}
		// 	// 	else
		// 	// 	{
		// 	// 		targetType = "key";
		// 	// 		targetString = targetNode.Key;
		// 	// 	}
		// 	//
		// 	// 	return true;
		// 	// }
		//
		// 	targetNode = default;
		// 	targetType = "";
		// 	targetString = ""; // TODO: Change to enum?
		// 	return false;
		// }
		//
		// private bool TryGetTraitInfo(string traitName, out TraitInfo traitInfo, bool addInfoSuffix = true)
		// {
		// 	// var searchString = addInfoSuffix ? $"{traitName}Info" : traitName;
		// 	// if (symbolCache.TraitInfos.ContainsKey(searchString))
		// 	// {
		// 	// 	traitInfo = symbolCache.TraitInfos[searchString];
		// 	// 	return true;
		// 	// }
		//
		// 	traitInfo = default;
		// 	return false;
		// }
	}
}
