using System;
using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;
using Oraide.LanguageServer.Caching;
using Oraide.LanguageServer.Extensions;

namespace Oraide.LanguageServer.LanguageServerProtocolHandlers.Workspace
{
	public class WorkspaceSymbolRequestHandler : BaseRpcMessageHandler
	{
		public WorkspaceSymbolRequestHandler(SymbolCache symbolCache, OpenFileCache openFileCache)
			: base(symbolCache, openFileCache) { }

		[OraideCustomJsonRpcMethodTag(Methods.WorkspaceSymbolName)]
		public IEnumerable<SymbolInformation> Symbols(WorkspaceSymbolParams request)
		{
			lock (LockObject)
			{
				try
				{
					if (trace)
						Console.Error.WriteLine("<-- Workspace-Symbols");

					var actors = symbolCache.ModSymbols.SelectMany(x => x.Value.ModSymbols.ActorDefinitions
						.SelectMany(actorDefinition => actorDefinition.First().ToSymbolInformation()));
					var weapons = symbolCache.ModSymbols.SelectMany(x => x.Value.ModSymbols.WeaponDefinitions
						.Select(weaponDefinition => weaponDefinition.First().ToSymbolInformation()));
					var conditions = symbolCache.ModSymbols.SelectMany(x => x.Value.ModSymbols.ConditionDefinitions
						.Select(condition => condition.First().ToSymbolInformation()));
					var cursors = symbolCache.ModSymbols.SelectMany(x => x.Value.ModSymbols.CursorDefinitions
						.Select(cursor => cursor.First().ToSymbolInformation()));
					var palettes = symbolCache.ModSymbols.SelectMany(x => x.Value.ModSymbols.PaletteDefinitions
						.Select(palette => palette.First().ToSymbolInformation()));
					var spriteSequenceImages = symbolCache.ModSymbols.SelectMany(x => x.Value.ModSymbols.SpriteSequenceImageDefinitions
						.Select(spriteSequenceImage => spriteSequenceImage.First().ToSymbolInformation()));

					return actors.Union(weapons).Union(conditions).Union(cursors).Union(palettes).Union(spriteSequenceImages);
				}
				catch (Exception e)
				{
					Console.Error.WriteLine("EXCEPTION!!!");
					Console.Error.WriteLine(e.ToString());
				}

				return null;
			}
		}
	}
}
