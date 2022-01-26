using System;
using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;
using Oraide.LanguageServer.Caching;
using Range = LspTypes.Range;

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
					{
						Console.Error.WriteLine("<-- TextDocument-References");
						Console.Error.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(positionParams));
					}

					if (TryGetCursorTarget(positionParams, out var target))
					{
						if (target.FileType == FileType.Rules)
						{
							if (target.TargetNodeIndentation == 1)
							{
								var traitDefinitions = symbolCache.ActorDefinitionsPerMod[target.ModId]
									.SelectMany(x =>
										x.SelectMany(y => y.Traits.Where(z => z.Name == target.TargetString)));

								return traitDefinitions.Select(x => new Location
								{
									Uri = new Uri(x.Location.FilePath).ToString(),
									Range = new Range
									{
										Start = new Position((uint)x.Location.LineNumber - 1, (uint)x.Location.CharacterPosition),
										End = new Position((uint)x.Location.LineNumber - 1, (uint)x.Location.CharacterPosition + (uint)target.TargetString.Length)
									}
								});
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
