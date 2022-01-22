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
	public class TextDocumentDefinitionHandler : BaseRpcMessageHandler
	{
		public TextDocumentDefinitionHandler(SymbolCache symbolCache, OpenFileCache openFileCache)
			: base(symbolCache, openFileCache) { }

		[OraideCustomJsonRpcMethodTag(Methods.TextDocumentDefinitionName)]
		public IEnumerable<Location> Definition(TextDocumentPositionParams positionParams)
		{
			lock (LockObject)
			{
				try
				{
					if (trace)
					{
						Console.Error.WriteLine("<-- TextDocument-Definition");
						Console.Error.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(positionParams));
					}

					if (TryGetCursorTarget(positionParams, out var target))
					{
						if (target.TargetType == "key"
						    && (target.TargetNodeIndentation == 1 || target.TargetNodeIndentation == 2)
						    && TryGetTargetCodeDefinitionLocations(target, out var codeDefinitionLocations))
							return codeDefinitionLocations;

						if ((target.TargetType == "value" || target.TargetNodeIndentation == 0)
						    && TryGetTargetYamlDefinitionLocations(target, out var yamlDefinitionLocations))
							return yamlDefinitionLocations;
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

		private bool TryGetTargetCodeDefinitionLocations(CursorTarget target, out IEnumerable<Location> definitionLocations)
		{
			if (!TryGetCodeMemberLocation(target.TargetNode, target.TargetString, out var traitInfo, out var location))
			{
				definitionLocations = default;
				return false;
			}

			definitionLocations = new[]
			{
				new Location
				{
					Uri = new Uri(location.FilePath).ToString(),
					Range = new Range
					{
						Start = new Position((uint) location.LineNumber - 1, (uint) location.CharacterPosition),
						End = new Position((uint) location.LineNumber - 1, (uint) location.CharacterPosition + (uint)target.TargetString.Length)
					}
				}
			};

			return true;
		}

		private bool TryGetTargetYamlDefinitionLocations(CursorTarget target, out IEnumerable<Location> definitionLocations)
		{
			// Check targetNode node type - probably via IndentationLevel enum.
			// If it is a top-level node *and this is an actor-definition or a weapon-definition file* it definitely is a definition.
			// If it is indented once we need to check if the target is the key or the value - keys are traits, but values *could* reference actor/weapon definitions.

			definitionLocations = symbolCache.ActorDefinitionsPerMod[target.ModId][target.TargetString].Select(x => new Location
				{
					Uri = new Uri(x.Location.FilePath).ToString(),
					Range = new Range
					{
						Start = new Position((uint)x.Location.LineNumber - 1, (uint)x.Location.CharacterPosition),
						End = new Position((uint)x.Location.LineNumber - 1, (uint)(x.Location.CharacterPosition + target.TargetString.Length))
					}
				})
				.Union(symbolCache.ConditionDefinitionsPerMod[target.ModId][target.TargetString].Select(x => new Location
				{
					Uri = new Uri(x.FilePath).ToString(),
					Range = new Range
					{
						Start = new Position((uint)x.LineNumber - 1, (uint) x.CharacterPosition),
						End = new Position((uint)x.LineNumber - 1, (uint)(x.CharacterPosition + target.TargetString.Length))
					}
				}));

			return true;
		}
	}
}
