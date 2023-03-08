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
	public class TextDocumentReferencesHandler : BaseRpcMessageHandler
	{
		public TextDocumentReferencesHandler(SymbolCache symbolCache, OpenFileCache openFileCache, IFileTypeHandlerConfiguration fileHandlerConfiguration)
			: base(symbolCache, openFileCache, fileHandlerConfiguration) { }

		[OraideCustomJsonRpcMethodTag(Methods.TextDocumentReferencesName)]
		public IEnumerable<Location> References(TextDocumentPositionParams positionParams)
		{
			lock (LockObject)
			{
				try
				{
					if (trace)
						Console.Error.WriteLine("<-- TextDocument-References");

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
			var service = fileHandlerConfiguration.GetService<IReferencesService>(cursorTarget);
			(service as BaseFileHandlingService)?.Initialize(cursorTarget);
			return service?.HandleReferences(cursorTarget);
		}
	}
}
