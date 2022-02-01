using System;
using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;
using Oraide.LanguageServer.Caching;
using Oraide.LanguageServer.Extensions;

namespace Oraide.LanguageServer.LanguageServerProtocolHandlers.TextDocument
{
	public class TextDocumentReferencesHandler : BaseRpcMessageHandler
	{
		public TextDocumentReferencesHandler(SymbolCache symbolCache, OpenFileCache openFileCache)
			: base(symbolCache, openFileCache) { }

		[OraideCustomJsonRpcMethodTag(Methods.TextDocumentReferencesName)]
		public IEnumerable<Location> References(TextDocumentPositionParams positionParams)
		{
			lock (LockObject)
			{
				try
				{
					if (trace)
						Console.Error.WriteLine("<-- TextDocument-References");

					if (TryGetCursorTarget(positionParams, out var target))
					{
						if (target.FileType == FileType.Rules)
						{
							if (target.TargetNodeIndentation == 1)
							{
								// Find where else the selected trait is used.
								return symbolCache[target.ModId].ActorDefinitions
									.SelectMany(x =>
										x.SelectMany(y => y.Traits.Where(z => z.Name == target.TargetString)))
									.Select(x => x.Location.ToLspLocation(target.TargetString.Length));
							}
						}
						else if (target.FileType == FileType.Weapons)
						{
							if (target.TargetNodeIndentation == 1)
							{
								if (target.TargetType == "key")
								{
									// TODO:
								}
								else if (target.TargetType == "value")
								{
									var targetNodeKey = target.TargetNode.Key;
									if (targetNodeKey == "Projectile")
									{
										// Find where else the selected projectile type is used.
										return symbolCache[target.ModId].WeaponDefinitions
											.SelectMany(x => x.Where(y => y.Projectile.Name == target.TargetString))
											.Select(x => x.Projectile.Location.ToLspLocation(target.TargetString.Length));
									}

									if (targetNodeKey == "Warhead" || targetNodeKey.StartsWith("Warhead@"))
									{
										// Find where else the selected warhead type is used.
										return symbolCache[target.ModId].WeaponDefinitions
											.SelectMany(x =>
												x.SelectMany(y => y.Warheads.Where(z => z.Name == target.TargetString)))
											.Select(x => x.Location.ToLspLocation(target.TargetString.Length));
									}
								}
							}
						}
					}
				}
				catch (Exception e)
				{
					Console.Error.WriteLine("EXCEPTION!!!");
					Console.Error.WriteLine(e.ToString());
				}

				return Enumerable.Empty<Location>();
			}
		}
	}
}
