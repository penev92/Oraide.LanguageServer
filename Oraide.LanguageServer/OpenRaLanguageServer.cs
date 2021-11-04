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

			using var rpc = JsonRpc.Attach(sendingStream, receivingStream, this);
			rpc.Disconnected += OnRpcDisconnected;
			await Task.Delay(-1);
		}

		void OnRpcDisconnected(object sender, JsonRpcDisconnectedEventArgs e)
		{
			Environment.Exit(0);
		}

		#region LanguageServerProtocol messagehandlers

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

		#endregion
	}
}
