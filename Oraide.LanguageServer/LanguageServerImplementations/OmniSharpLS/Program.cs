using System;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Oraide.LanguageServer.LanguageServerImplementations.OmniSharpLS
{
	class Program
	{
		// public class DefinitionHandler : IDefinitionHandler
		// {
		//	 public async Task<LocationOrLocationLinks> Handle(DefinitionParams request, CancellationToken cancellationToken)
		//	 {
		//		 throw new NotImplementedException();
		//	 }
		//
		//	 public DefinitionRegistrationOptions GetRegistrationOptions(DefinitionCapability capability,
		//		 ClientCapabilities clientCapabilities)
		//	 {
		//		 throw new NotImplementedException();
		//	 }
		// }
		//
		// public class DefinitionThingyHandler : DefinitionHandlerBase
		// {
		//	 protected override DefinitionRegistrationOptions CreateRegistrationOptions(DefinitionCapability capability,
		//		 ClientCapabilities clientCapabilities)
		//	 {
		//		 throw new NotImplementedException();
		//	 }
		//
		//	 public override async Task<LocationOrLocationLinks> Handle(DefinitionParams request, CancellationToken cancellationToken)
		//	 {
		//		 throw new NotImplementedException();
		//	 }
		// }

		internal class FoldingRangeHandler : IFoldingRangeHandler
		{
			public FoldingRangeRegistrationOptions GetRegistrationOptions() =>
				new FoldingRangeRegistrationOptions
				{
					DocumentSelector = DocumentSelector.ForLanguage("csharp")
				};

			public Task<Container<FoldingRange>?> Handle(
				FoldingRangeRequestParam request,
				CancellationToken cancellationToken
			) =>
				Task.FromResult<Container<FoldingRange>?>(
					new Container<FoldingRange>(
						new FoldingRange
						{
							StartLine = 10,
							EndLine = 20,
							Kind = FoldingRangeKind.Region,
							EndCharacter = 0,
							StartCharacter = 0
						}
					)
				);

			public FoldingRangeRegistrationOptions GetRegistrationOptions(FoldingRangeCapability capability, ClientCapabilities clientCapabilities) => new FoldingRangeRegistrationOptions
			{
				DocumentSelector = DocumentSelector.ForLanguage("csharp")
			};
		}

		// 19.0-19.5
		public class DefinitionHandler : DefinitionHandlerBase
		{
			protected override DefinitionRegistrationOptions CreateRegistrationOptions(DefinitionCapability capability,
				ClientCapabilities clientCapabilities)
			{
				throw new NotImplementedException();
			}

			public override async Task<LocationOrLocationLinks> Handle(DefinitionParams request, CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}
		}

		// 18.3
		// public class DefinitionHandler : IDefinitionHandler
		// {
		//	 public async Task<LocationOrLocationLinks> Handle(DefinitionParams request, CancellationToken cancellationToken)
		//	 {
		//		 throw new NotImplementedException();
		//	 }
		//
		//	 public DefinitionRegistrationOptions GetRegistrationOptions()
		//	 {
		//		 throw new NotImplementedException();
		//	 }
		//
		//	 public void SetCapability(DefinitionCapability capability)
		//	 {
		//		 throw new NotImplementedException();
		//	 }
		// }

		// 18.0
		// public class DefinitionHandler : IDefinitionHandler
		// {
		//	 public async Task<LocationOrLocationLinks> Handle(DefinitionParams request, CancellationToken cancellationToken)
		//	 {
		//		 throw new NotImplementedException();
		//	 }
		//
		//	 public DefinitionRegistrationOptions GetRegistrationOptions()
		//	 {
		//		 throw new NotImplementedException();
		//	 }
		//
		//	 public void SetCapability(DefinitionCapability capability)
		//	 {
		//		 throw new NotImplementedException();
		//	 }
		// }

		// 16.0
		// public class  DefinitionHandler : IDefinitionHandler
		// {
		//	 public async Task<LocationOrLocationLinks> Handle(DefinitionParams request, CancellationToken cancellationToken)
		//	 {
		//		 throw new NotImplementedException();
		//	 }
		//
		//	 public DefinitionRegistrationOptions GetRegistrationOptions()
		//	 {
		//		 throw new NotImplementedException();
		//	 }
		//
		//	 public void SetCapability(DefinitionCapability capability)
		//	 {
		//		 throw new NotImplementedException();
		//	 }
		// }


		// 14.0
		// public class  DefinitionHandler : IDefinitionHandler
		// {
		//	 public async Task<LocationOrLocationLinks> Handle(DefinitionParams request, CancellationToken cancellationToken)
		//	 {
		//		 throw new NotImplementedException();
		//	 }
		//
		//	 public TextDocumentRegistrationOptions GetRegistrationOptions()
		//	 {
		//		 throw new NotImplementedException();
		//	 }
		//
		//	 public void SetCapability(DefinitionCapability capability)
		//	 {
		//		 throw new NotImplementedException();
		//	 }
		// }

		// 10.0
		// public class DefinitionHandler : IDefinitionHandler
		// {
		//	 public async Task<LocationOrLocations> Handle(DefinitionParams request, CancellationToken cancellationToken)
		//	 {
		//		 throw new NotImplementedException();
		//	 }
		//
		//	 public TextDocumentRegistrationOptions GetRegistrationOptions()
		//	 {
		//		 throw new NotImplementedException();
		//	 }
		//
		//	 public void SetCapability(DefinitionCapability capability)
		//	 {
		//		 throw new NotImplementedException();
		//	 }
		// }

		// private static void Main(string[] args) => MainAsync(args).Wait();

		public static async Task MainAsync(string[] args)
		{
			// OmniSharp.Extensions.LanguageServer.Server.LanguageServer.Create(options =>
			//	 options.OnHover(Handler, RegistrationOptions));
			var server = await OmniSharp.Extensions.LanguageServer.Server.LanguageServer.From(options =>
				options
					.WithInput(Console.OpenStandardInput())
					.WithOutput(Console.OpenStandardOutput())
					// .WithHandler<TextDocumentHandler>()
					// .WithHandler<DidChangeWatchedFilesHandler>()
					// .WithHandler<FoldingRangeHandler>()
					// .WithHandler<MyWorkspaceSymbolsHandler>()
					// .WithHandler<MyDocumentSymbolHandler>()
					// .WithHandler<SemanticTokensHandler>()

					// .WithLoggerFactory(new LoggerFactory())
					// .AddDefaultLoggingProvider()
					// .WithMinimumLogLevel(LogLevel.Trace)
					// .WithServices(ConfigureServices)
					.WithHandler<DefinitionHandler>()
					.WithHandler<FoldingRangeHandler>()
			// .WithHandler<DefinitionThingyHandler>()
			// .WithHandler<TextDocumentSyncHandler>()

			// .WithHandler<HoverHandler>()
			// .OnHover(Handler, RegistrationOptions)
			// .WithLoggerFactory(new LoggerFactory())
			// .AddDefaultLoggingProvider()
			// .WithMinimumLogLevel(LogLevel.Trace)
			// .WithServices(ConfigureServices)
			// .WithHandler<TextDocumentSyncHandler>()
			);//.ConfigureAwait(false);
			Console.WriteLine("wow");

			await server.WaitForExit;
		}

		// static void ConfigureServices(IServiceCollection services)
		// {
		//	 services.AddSingleton<BufferManager>();
		// }

		// private static HoverRegistrationOptions RegistrationOptions(HoverCapability capability, ClientCapabilities clientcapabilities)
		// {
		//	 throw new NotImplementedException();
		// }
		//
		// private static Task<Hover?> Handler(HoverParams arg)
		// {
		//	 throw new NotImplementedException();
		// }
	}
}
