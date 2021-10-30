using System;
using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;
using Range = LspTypes.Range;

namespace Oraide.LanguageServer.LanguageServerImplementations.Kaby76.Implementation
{
	public partial class LSPServer
	{
		[JsonRpcMethod(Methods.TextDocumentDefinitionName)]
		public IEnumerable<Location> Definition(JToken arg)
		{
			lock (_object)
			{
				try
				{
					if (trace)
					{
						Console.Error.WriteLine("<-- TextDocument-Definition");
						Console.Error.WriteLine(arg.ToString());
					}

					var request = arg.ToObject<TextDocumentPositionParams>();
					if (TryGetTargetNode(request, out var targetNode))
					{
						if (TryGetTargetCodeDefinitionLocations(targetNode, out var codeDefinitionLocations))
							return codeDefinitionLocations;

						if (TryGetTargetYamlDefinitionLocations(targetNode, out var yamlDefinitionLocations))
							return yamlDefinitionLocations;
					}
				}
				catch (Exception)
				{
				}

				return Enumerable.Empty<Location>();
			}
		}

		private bool TryGetTargetCodeDefinitionLocations(OpenRA.MiniYamlParser.MiniYamlNode targetNode, out IEnumerable<Location> definitionLocations)
		{
			var traitName = targetNode.Key.Split('@')[0];
			if (!TryGetTraitInfo(traitName, out var traitInfo))
			{
				definitionLocations = default;
				return false;
			}

			definitionLocations = new[]
			{
				new Location
				{
					Uri = new Uri(traitInfo.Location.FilePath).ToString(),
					Range = new Range
					{
						Start = new Position((uint)traitInfo.Location.LineNumber, (uint)traitInfo.Location.CharacterPosition),
						End = new Position((uint)traitInfo.Location.LineNumber, (uint)traitInfo.Location.CharacterPosition + 5)
					}
				}
			};

			return true;
		}

		private bool TryGetTargetYamlDefinitionLocations(OpenRA.MiniYamlParser.MiniYamlNode targetNode, out IEnumerable<Location> definitionLocations)
		{
			// Check targetNode node type - probably via IndentationLevel enum.
			// If it is a top-level node *and this is an actor-definition or a weapon-definition file* it definitely is a definition.
			// If it is indented once we need to check if the target is the key or the value - keys are traits, but values *could* reference actor/weapon definitions.

			definitionLocations = actorDefinitions[targetNode.Key].Select(x => new Location
			{
				Uri = new Uri(x.Location.FilePath).ToString(),
				Range = new Range
				{
					Start = new Position((uint)x.Location.LineNumber - 1, (uint)x.Location.CharacterPosition),
					End = new Position((uint)x.Location.LineNumber - 1, (uint)x.Location.CharacterPosition + 5)
				}
			});

			return true;
		}
	}
}
