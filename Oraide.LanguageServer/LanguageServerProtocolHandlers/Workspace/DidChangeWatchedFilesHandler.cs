using System;
using LspTypes;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;

namespace Oraide.LanguageServer.LanguageServerProtocolHandlers.Workspace
{
	// This event is triggered whenever a file in the current workspace changes regardless of whether it's open or not and what changed it.
	public class DidChangeWatchedFilesHandler : BaseRpcMessageHandler
	{
		public DidChangeWatchedFilesHandler(SymbolCache symbolCache)
			: base(symbolCache) { }

		[OraideCustomJsonRpcMethodTag(Methods.WorkspaceDidChangeWatchedFilesName)]
		public void DidChangeWatchedFiles(DidChangeWatchedFilesParams request)
		{
			lock (LockObject)
			{
				try
				{
					if (trace)
					{
						Console.Error.WriteLine("<-- Workspace-DidChangeWatchedFiles");
						Console.Error.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(request));
					}

					// TODO: Rebuild all cached collections of YAML symbols.
					// TODO: Check if the affected file(s) are present in the "currently opened files" cache and invalidate/update that.
				}
				catch (Exception e)
				{
				}
			}
		}
	}
}
