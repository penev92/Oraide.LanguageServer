using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LspTypes;
using Newtonsoft.Json.Linq;
using Oraide.LanguageServer.Abstractions;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;
using StreamJsonRpc;

namespace Oraide.LanguageServer
{
	public class OpenRaLanguageServer : ILanguageServer
	{
		private readonly IReadOnlyDictionary<string, (IRpcMessageHandler MessageHandlerClass, MethodInfo MethodInfo)> rpcMessageHandlers;

		public OpenRaLanguageServer(IEnumerable<IRpcMessageHandler> messageHandlers)
		{
			rpcMessageHandlers = messageHandlers
				.SelectMany(x => x.GetType().GetMethods()
					.Select(y => new KeyValuePair<string, (IRpcMessageHandler MessageHandlerClass, MethodInfo MethodInfo)>(y.CustomAttributes
						.FirstOrDefault(z => z.AttributeType.Name == nameof(OraideCustomJsonRpcMethodTagAttribute))?.ConstructorArguments.FirstOrDefault().Value.ToString(), (x, y)))
					.Where(y => y.Key != null))
				.ToDictionary(x => x.Key, y => y.Value);
		}

		public async Task RunAsync()
		{
			var sendingStream = Console.OpenStandardOutput();
			var receivingStream = Console.OpenStandardInput();

			// Comment out to disable verbose editor-server message logging.
			receivingStream = new Tee(receivingStream, new Dup("EDITOR"), Tee.StreamOwnership.OwnNone);
			sendingStream = new Tee(sendingStream, new Dup("SERVER"), Tee.StreamOwnership.OwnNone);

			using var rpc = JsonRpc.Attach(sendingStream, receivingStream, this);
			rpc.Disconnected += OnRpcDisconnected;
			await Task.Delay(-1);
		}

		void OnRpcDisconnected(object sender, JsonRpcDisconnectedEventArgs e)
		{
			Environment.Exit(0);
		}

		#region LanguageServerProtocol message handlers

		#region General messages

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

		#endregion

		#region TextDocument messages

		[JsonRpcMethod(Methods.TextDocumentCompletionName)]
		public CompletionList CompletionTextDocument(JToken arg)
		{
			var (messageHandlerClass, methodInfo) = rpcMessageHandlers[Methods.TextDocumentCompletionName];
			return (CompletionList)methodInfo.Invoke(messageHandlerClass, new object[] { arg.ToObject<CompletionParams>() });
		}

		[JsonRpcMethod(Methods.TextDocumentDefinitionName)]
		public IEnumerable<Location> Definition(JToken arg)
		{
			var (messageHandlerClass, methodInfo) = rpcMessageHandlers[Methods.TextDocumentDefinitionName];
			return (IEnumerable<Location>)methodInfo.Invoke(messageHandlerClass, new object[] { arg.ToObject<TextDocumentPositionParams>() });
		}

		// The currently used LSPTypes library doesn't even have support for this method, so hardcoding the method name.
		[JsonRpcMethod("textDocument/declaration")]
		public IEnumerable<Location> Declaration(JToken arg)
		{
			// Luckily, for our purposes there isn't a case where definition != declaration, so just reuse Definition() here.
			return Definition(arg);
		}

		[JsonRpcMethod(Methods.TextDocumentImplementationName)]
		public IEnumerable<Location> Implementation(JToken arg)
		{
			// We will just assume that for our purposes there isn't a case where definition != implementation and just reuse Definition() here.
			return Definition(arg);
		}

		[JsonRpcMethod(Methods.TextDocumentHoverName)]
		public Hover Hover(JToken arg)
		{
			var (messageHandlerClass, methodInfo) = rpcMessageHandlers[Methods.TextDocumentHoverName];
			return (Hover)methodInfo.Invoke(messageHandlerClass, new object[] { arg.ToObject<TextDocumentPositionParams>() });
		}

		[JsonRpcMethod(Methods.TextDocumentDidOpenName)]
		public void DidOpenTextDocument(JToken arg)
		{
			var (messageHandlerClass, methodInfo) = rpcMessageHandlers[Methods.TextDocumentDidOpenName];
			methodInfo.Invoke(messageHandlerClass, new object[] { arg.ToObject<DidOpenTextDocumentParams>() });
		}

		[JsonRpcMethod(Methods.TextDocumentDidChangeName)]
		public void DidChangeTextDocument(JToken arg)
		{
			var (messageHandlerClass, methodInfo) = rpcMessageHandlers[Methods.TextDocumentDidChangeName];
			methodInfo.Invoke(messageHandlerClass, new object[] { arg.ToObject<DidChangeTextDocumentParams>() });
		}

		[JsonRpcMethod(Methods.TextDocumentDidCloseName)]
		public void DidCloseTextDocument(JToken arg)
		{
			var (messageHandlerClass, methodInfo) = rpcMessageHandlers[Methods.TextDocumentDidCloseName];
			methodInfo.Invoke(messageHandlerClass, new object[] { arg.ToObject<DidCloseTextDocumentParams>() });
		}

		#endregion

		#region Workspace messages

		[JsonRpcMethod(Methods.WorkspaceDidChangeWatchedFilesName)]
		public void DidChangeWatchedFiles(JToken arg)
		{
			var (messageHandlerClass, methodInfo) = rpcMessageHandlers[Methods.WorkspaceDidChangeWatchedFilesName];
			methodInfo.Invoke(messageHandlerClass, new object[] { arg.ToObject<DidChangeWatchedFilesParams>() });
		}

		#endregion

		#endregion
	}
}
