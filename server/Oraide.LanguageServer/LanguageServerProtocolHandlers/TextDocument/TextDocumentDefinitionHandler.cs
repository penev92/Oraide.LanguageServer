using System;
using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.FileHandlingServices;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers.Configuration;
using Oraide.LanguageServer.Caching;

namespace Oraide.LanguageServer.LanguageServerProtocolHandlers.TextDocument
{
	public class TextDocumentDefinitionHandler : BaseRpcMessageHandler
	{
		public TextDocumentDefinitionHandler(SymbolCache symbolCache, OpenFileCache openFileCache, IFileTypeHandlerConfiguration fileHandlerConfiguration)
			: base(symbolCache, openFileCache, fileHandlerConfiguration) { }

		[OraideCustomJsonRpcMethodTag(Methods.TextDocumentDefinitionName)]
		public IEnumerable<Location> Definition(TextDocumentPositionParams positionParams)
		{
			lock (LockObject)
			{
				try
				{
					if (trace)
						Console.Error.WriteLine("<-- TextDocument-Definition");

					return HandlePositionalRequest(positionParams) as IEnumerable<Location>;
				}
				catch (Exception e)
				{
					Console.Error.WriteLine("EXCEPTION!!!");
					Console.Error.WriteLine(e.ToString());
				}

				return Enumerable.Empty<Location>();
			}
		}

		protected override IEnumerable<Location> HandlePositionalRequestInner(CursorTarget cursorTarget)
		{
			var service = fileHandlerConfiguration.GetService<IDefinitionService>(cursorTarget);
			(service as BaseFileHandlingService)?.Initialize(cursorTarget);
			return service?.HandleDefinition(cursorTarget);
		}
	}
}
