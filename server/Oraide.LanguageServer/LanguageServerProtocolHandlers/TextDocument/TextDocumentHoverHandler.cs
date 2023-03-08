using System;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.FileHandlingServices;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers.Configuration;
using Oraide.LanguageServer.Caching;

namespace Oraide.LanguageServer.LanguageServerProtocolHandlers.TextDocument
{
	public class TextDocumentHoverHandler : BaseRpcMessageHandler
	{
		public TextDocumentHoverHandler(SymbolCache symbolCache, OpenFileCache openFileCache, IFileTypeHandlerConfiguration fileHandlerConfiguration)
			: base(symbolCache, openFileCache, fileHandlerConfiguration) { }

		[OraideCustomJsonRpcMethodTag(Methods.TextDocumentHoverName)]
		public Hover Hover(TextDocumentPositionParams positionParams)
		{
			lock (LockObject)
			{
				try
				{
					if (trace)
						Console.Error.WriteLine("<-- TextDocument-Hover");

					return HandlePositionalRequest(positionParams) as Hover;
				}
				catch (Exception e)
				{
					Console.Error.WriteLine("EXCEPTION!!!");
					Console.Error.WriteLine(e.ToString());
				}

				return null;
			}
		}

		protected override Hover HandlePositionalRequestInner(CursorTarget cursorTarget)
		{
			var service = fileHandlerConfiguration.GetService<IHoverService>(cursorTarget);
			(service as BaseFileHandlingService)?.Initialize(cursorTarget);
			return service?.HandleHover(cursorTarget);
		}
	}
}
