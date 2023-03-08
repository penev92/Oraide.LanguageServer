using System;
using System.IO;
using System.Linq;
using LspTypes;
using Oraide.Core;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;
using Oraide.LanguageServer.Caching;

namespace Oraide.LanguageServer.LanguageServerProtocolHandlers.TextDocument
{
	// This event is only triggered when a file is opened or a tab is switched to for the first time. Subsequent switching to an already opened tab will not trigger it.
	public class TextDocumentDidOpenHandler : BaseRpcMessageHandler
	{
		public TextDocumentDidOpenHandler(SymbolCache symbolCache, OpenFileCache openFileCache)
			: base(symbolCache, openFileCache, null) { }

		[OraideCustomJsonRpcMethodTag(Methods.TextDocumentDidOpenName)]
		public void DidOpenTextDocument(DidOpenTextDocumentParams request)
		{
			lock (LockObject)
			{
				try
				{
					if (trace)
						Console.Error.WriteLine("<-- TextDocument-DidOpen");

					// HACK HACK HACK!!!
					// For whatever reason we receive the file URI borked - looks to be encoded for JSON, but the deserialization doesn't fix it.
					// No idea if this is an issue with VSCode or the LSP library used as there are currently no clients for other text editors.
					var incomingFileUriString = OpenRaFolderUtils.NormalizeFileUriString(request.TextDocument.Uri);

					openFileCache.AddOrUpdateOpenFile(incomingFileUriString, request.TextDocument.Text);

					// Check if this is a map-related file and potentially load the map into the symbol cache.
					TryGetModId(incomingFileUriString, out var modId);
					var fileUri = new Uri(incomingFileUriString);

					var modManifest = symbolCache[modId].ModManifest;
					var filePath = fileUri.AbsolutePath;
					var fileName = filePath.Split($"mods/{modId}/")[1];
					var fileReference = $"{modId}|{fileName}";

					if (!modManifest.RulesFiles.Contains(fileReference)
						&& !modManifest.WeaponsFiles.Contains(fileReference)
						&& !modManifest.CursorsFiles.Contains(fileReference))
					{
						var targetFileDir = OpenRaFolderUtils.NormalizeFilePathString(Path.GetDirectoryName(filePath));
						MapManifest mapManifest = default;
						if (Path.GetFileName(filePath) == "map.yaml")
							mapManifest = symbolCache[modId].Maps.FirstOrDefault(x => x.MapFolder == targetFileDir);

						if (string.IsNullOrWhiteSpace(mapManifest.MapFolder))
							mapManifest = symbolCache[modId].Maps.FirstOrDefault(x => x.RulesFiles.Contains(fileReference));

						if (string.IsNullOrWhiteSpace(mapManifest.MapFolder))
							mapManifest = symbolCache[modId].Maps.FirstOrDefault(x => x.WeaponsFiles.Contains(fileReference));

						if (!string.IsNullOrWhiteSpace(mapManifest.MapFolder) && !symbolCache.Maps.ContainsKey(mapManifest.MapReference))
							symbolCache.AddMap(modId, mapManifest);
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
