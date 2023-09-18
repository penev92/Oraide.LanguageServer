using System;
using System.IO;
using System.Linq;
using LspTypes;
using Oraide.Core;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;
using Oraide.LanguageServer.Caching;

namespace Oraide.LanguageServer.LanguageServerProtocolHandlers.Workspace
{
	// This event is triggered whenever a file in the current workspace changes regardless of whether it's open or not and what changed it.
	public class DidChangeWatchedFilesHandler : BaseRpcMessageHandler
	{
		public DidChangeWatchedFilesHandler(SymbolCache symbolCache, OpenFileCache openFileCache)
			: base(symbolCache, openFileCache, null) { }

		[OraideCustomJsonRpcMethodTag(Methods.WorkspaceDidChangeWatchedFilesName)]
		public void DidChangeWatchedFiles(DidChangeWatchedFilesParams request)
		{
			lock (LockObject)
			{
				try
				{
					if (trace)
						Console.Error.WriteLine($"[{DateTime.Now:hh:mm:ss.fff}] Workspace-DidChangeWatchedFiles");

					// TODO: Be smarter about which symbol collections we actually need to update so we don't do all.
					symbolCache.Update();

					foreach (var requestChange in request.Changes)
					{
						// HACK HACK HACK!!!
						// For whatever reason we receive the file URI borked - looks to be encoded for JSON, but the deserialization doesn't fix it.
						// No idea if this is an issue with VSCode or the LSP library used as there are currently no clients for other text editors.
						var incomingFileUriString = OpenRaFolderUtils.NormalizeFileUriString(requestChange.Uri);

						// Check if the affected file(s) are present in the "currently opened files" cache and invalidate/update that.
						if (openFileCache.ContainsFile(incomingFileUriString))
						{
							if(requestChange.FileChangeType == FileChangeType.Deleted)
								openFileCache.RemoveOpenFile(incomingFileUriString);
							else if (requestChange.FileChangeType == FileChangeType.Changed)
								openFileCache.AddOrUpdateOpenFile(incomingFileUriString);
						}

						// Check if this is a map-related file and potentially load the map into the symbol cache.
						TryGetModId(incomingFileUriString, out var modId);
						var fileUri = new Uri(incomingFileUriString);

						if (!symbolCache.ModSymbols.ContainsKey(modId))
							continue;

						var modManifest = symbolCache[modId].ModManifest;
						var filePath = fileUri.AbsolutePath;
						var fileName = filePath.Split($"mods/{modId}/")[1];
						var fileReference = $"{modId}|{fileName}";

						if (!modManifest.RulesFiles.Contains(fileReference)
						    && !modManifest.WeaponsFiles.Contains(fileReference)
						    && !modManifest.CursorsFiles.Contains(fileReference))
						{
							var targetFileDir = Path.GetDirectoryName(filePath);
							MapManifest mapManifest = default;
							if (Path.GetFileName(filePath) == "map.yaml")
								mapManifest = symbolCache[modId].Maps.FirstOrDefault(x => x.MapFolder == targetFileDir);

							if (string.IsNullOrWhiteSpace(mapManifest.MapFolder))
								mapManifest = symbolCache[modId].Maps.FirstOrDefault(x => x.RulesFiles.Contains(fileReference));

							if (string.IsNullOrWhiteSpace(mapManifest.MapFolder))
								mapManifest = symbolCache[modId].Maps.FirstOrDefault(x => x.WeaponsFiles.Contains(fileReference));

							if (!string.IsNullOrWhiteSpace(mapManifest.MapFolder) && symbolCache.Maps.ContainsKey(mapManifest.MapReference))
							{
								var specificMap = symbolCache[modId].Maps.FirstOrDefault(x => fileReference.StartsWith(x.MapReference));
								if (!string.IsNullOrWhiteSpace(specificMap.MapFolder))
									symbolCache.UpdateMap(modId, mapManifest);
								else
									foreach (var map in symbolCache[modId].Maps)
										symbolCache.UpdateMap(modId, map);
							}
						}
					}
				}
				catch (Exception e)
				{
					Console.Error.WriteLine("EXCEPTION!!!");
					Console.Error.WriteLine(e.ToString());
				}
			}
		}
	}
}
