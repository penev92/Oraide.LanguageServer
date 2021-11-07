using System;
using LspTypes;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;
using Oraide.LanguageServer.Caching;

namespace Oraide.LanguageServer.LanguageServerProtocolHandlers.Workspace
{
	// This event is triggered whenever a file in the current workspace changes regardless of whether it's open or not and what changed it.
	public class DidChangeWatchedFilesHandler : BaseRpcMessageHandler
	{
		public DidChangeWatchedFilesHandler(SymbolCache symbolCache, OpenFileCache openFileCache)
			: base(symbolCache, openFileCache) { }

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

					// Rebuild all cached collections of YAML symbols.
					// TODO: Be smarter about which symbol collections we actually need to update so we don't do all.
					symbolCache.UpdateYamlSymbols();

					// Check if the affected file(s) are present in the "currently opened files" cache and invalidate/update that.
					foreach (var requestChange in request.Changes)
					{
						if (openFileCache.ContainsFile(requestChange.Uri))
						{
							if(requestChange.FileChangeType == FileChangeType.Deleted)
								openFileCache.RemoveOpenFile(requestChange.Uri);
							else if (requestChange.FileChangeType == FileChangeType.Changed)
								openFileCache.AddOrUpdateOpenFile(requestChange.Uri);
						}
					}
				}
				catch (Exception e)
				{
				}
			}
		}
	}
}
