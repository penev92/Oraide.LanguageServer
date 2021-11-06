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
						if (TryGetTargetCodeDefinitionLocations(target.TargetNode, target.TargetType, target.TargetString, out var codeDefinitionLocations))
							return codeDefinitionLocations;

						if (TryGetTargetYamlDefinitionLocations(target.TargetNode, target.TargetType, target.TargetString, out var yamlDefinitionLocations))
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

		private bool TryGetTargetCodeDefinitionLocations(YamlNode targetNode, string targetType, string targetString, out IEnumerable<Location> definitionLocations)
		{
			if (!TryGetCodeMemberLocation(targetNode, targetString, out var traitInfo, out var location))
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
						End = new Position((uint) location.LineNumber - 1, (uint) location.CharacterPosition + (uint)targetString.Length)
					}
				}
			};

			return true;
		}

		private bool TryGetTargetYamlDefinitionLocations(YamlNode targetNode, string targetType, string targetString,
			out IEnumerable<Location> definitionLocations)
		{
			// Check targetNode node type - probably via IndentationLevel enum.
			// If it is a top-level node *and this is an actor-definition or a weapon-definition file* it definitely is a definition.
			// If it is indented once we need to check if the target is the key or the value - keys are traits, but values *could* reference actor/weapon definitions.

			definitionLocations = new List<Location>();
			definitionLocations = symbolCache.ActorDefinitions[targetString].Select(x => new Location
				{
					Uri = new Uri(x.Location.FilePath).ToString(),
					Range = new Range
					{
						Start = new Position((uint) x.Location.LineNumber - 1, (uint) x.Location.CharacterPosition),
						End = new Position((uint) x.Location.LineNumber - 1, (uint) x.Location.CharacterPosition + 5)
					}
				})
				.Union(symbolCache.ConditionDefinitions[targetString].Select(x => new Location
				{
					Uri = new Uri(x.FilePath).ToString(),
					Range = new Range
					{
						Start = new Position((uint) x.LineNumber - 1, (uint) x.CharacterPosition),
						End = new Position((uint) x.LineNumber - 1, (uint) x.CharacterPosition + 5)
					}
				}));

			return true;
		}
	}
}
