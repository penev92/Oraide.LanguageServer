using System;
using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;
using Oraide.LanguageServer.Caching;
using Range = LspTypes.Range;

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

					var actors = symbolCache.ActorDefinitionsPerMod
						.SelectMany(x => x.Value)
						.Select(actorDefinition =>
						{
							var loc = actorDefinition.First().Location;
							return new SymbolInformation
							{
								Name = actorDefinition.Key,
								Kind = SymbolKind.Struct,
								Tags = Array.Empty<SymbolTag>(),
								Location = new Location
								{
									Uri = new Uri(loc.FilePath).ToString(),
									Range = new Range
									{
										Start = new Position((uint) loc.LineNumber - 1, (uint) loc.CharacterPosition),
										End = new Position((uint)loc.LineNumber - 1, (uint)loc.CharacterPosition + (uint)actorDefinition.Key.Length)
									}
								}
							};
						});

					var weapons = symbolCache.WeaponDefinitionsPerMod
						.SelectMany(x => x.Value)
						.Select(weaponDefinition =>
						{
							var loc = weaponDefinition.First().Location;
							return new SymbolInformation
							{
								Name = weaponDefinition.Key,
								Kind = SymbolKind.Struct,
								Tags = Array.Empty<SymbolTag>(),
								Location = new Location
								{
									Uri = new Uri(loc.FilePath).ToString(),
									Range = new Range
									{
										Start = new Position((uint)loc.LineNumber - 1, (uint)loc.CharacterPosition),
										End = new Position((uint)loc.LineNumber - 1, (uint)loc.CharacterPosition + (uint)weaponDefinition.Key.Length)
									}
								}
							};
						});

					var conditions = symbolCache.ConditionDefinitionsPerMod
						.SelectMany(x => x.Value)
						.Select(condition =>
						{
							var loc = condition.First();
							return new SymbolInformation
							{
								Name = condition.Key,
								Kind = SymbolKind.String,
								Tags = Array.Empty<SymbolTag>(),
								Location = new Location
								{
									Uri = new Uri(loc.FilePath).ToString(),
									Range = new Range
									{
										Start = new Position((uint)loc.LineNumber - 1, (uint)loc.CharacterPosition),
										End = new Position((uint)loc.LineNumber - 1, (uint)loc.CharacterPosition + (uint)condition.Key.Length)
									}
								}
							};
						});

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
