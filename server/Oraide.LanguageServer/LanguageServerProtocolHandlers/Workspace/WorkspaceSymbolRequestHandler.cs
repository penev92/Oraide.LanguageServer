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
					{
						Console.Error.WriteLine("<-- Workspace-Symbols");
						Console.Error.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(request));
					}

					var actors = symbolCache.ModSymbols.SelectMany(x => x.Value.ActorDefinitions
						.Select(actorDefinition =>
						{
							var loc = actorDefinition.First().Location;
							return new SymbolInformation
							{
								Name = actorDefinition.Key,
								Kind = SymbolKind.Struct,
								Tags = Array.Empty<SymbolTag>(),
								Location = loc.ToLspLocation(actorDefinition.Key.Length)
							};
						}));

					var weapons = symbolCache.ModSymbols.SelectMany(x => x.Value.WeaponDefinitions
						.Select(weaponDefinition =>
						{
							var loc = weaponDefinition.First().Location;
							return new SymbolInformation
							{
								Name = weaponDefinition.Key,
								Kind = SymbolKind.Struct,
								Tags = Array.Empty<SymbolTag>(),
								Location = loc.ToLspLocation(weaponDefinition.Key.Length)
							};
						}));

					var conditions = symbolCache.ModSymbols.SelectMany(x => x.Value.ConditionDefinitions
						.Select(condition =>
						{
							var loc = condition.First().Location;
							return new SymbolInformation
							{
								Name = condition.Key,
								Kind = SymbolKind.String,
								Tags = Array.Empty<SymbolTag>(),
								Location = loc.ToLspLocation(condition.Key.Length)
							};
						}));

					return actors.Union(weapons).Union(conditions);
				}
				catch (Exception e)
				{
				}

				return null;
			}
		}
	}
}
