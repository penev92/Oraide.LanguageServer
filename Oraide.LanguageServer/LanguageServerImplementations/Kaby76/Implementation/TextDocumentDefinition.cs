using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LspTypes;
using Newtonsoft.Json.Linq;
using Oraide.Core.Entities;
using Oraide.Core.Entities.Csharp;
using Oraide.Core.Entities.MiniYaml;
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
						// Console.Error.WriteLine(arg.ToString());
					}

					var request = arg.ToObject<TextDocumentPositionParams>();
					if (TryGetTargetNode(request, out var targetNode, out var targetType, out var targetString))
					{
						if (TryGetTargetCodeDefinitionLocations(targetNode, targetType, targetString, out var codeDefinitionLocations))
							return codeDefinitionLocations;

						if (TryGetTargetYamlDefinitionLocations(targetNode, targetType, targetString, out var yamlDefinitionLocations))
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
			MemberLocation? location = null;

			// Try treating the target string as a trait name.
			var traitName = targetString.Split('@')[0];
			if (TryGetTraitInfo(traitName, out var traitInfo))
				location = traitInfo.Location;
			else
			{
				// Assuming we are targeting a trait property, search for a trait based on the parent node.
				traitName = targetNode.ParentNode?.Key.Split('@')[0];
				if (TryGetTraitInfo(traitName, out traitInfo))
				{
					if (CheckTraitInheritanceTree(traitInfo, targetString, out var propertyLocation))
					{
						location = propertyLocation;
						var a = 1;
					}
				}
			}

			if (location == null)
			{
				definitionLocations = default;
				return false;
			}

			definitionLocations = new[]
			{
				new Location
				{
					Uri = new Uri(location.Value.FilePath).ToString(),
					Range = new Range
					{
						Start = new Position((uint)location.Value.LineNumber, (uint)location.Value.CharacterPosition),
						End = new Position((uint)location.Value.LineNumber, (uint)location.Value.CharacterPosition + 5)
					}
				}
			};

			return true;
		}

		private bool TryGetTargetYamlDefinitionLocations(YamlNode targetNode, string targetType, string targetString, out IEnumerable<Location> definitionLocations)
		{
			// Check targetNode node type - probably via IndentationLevel enum.
			// If it is a top-level node *and this is an actor-definition or a weapon-definition file* it definitely is a definition.
			// If it is indented once we need to check if the target is the key or the value - keys are traits, but values *could* reference actor/weapon definitions.

			definitionLocations = actorDefinitions[targetString].Select(x => new Location
				{
					Uri = new Uri(x.Location.FilePath).ToString(),
					Range = new Range
					{
						Start = new Position((uint) x.Location.LineNumber - 1, (uint) x.Location.CharacterPosition),
						End = new Position((uint) x.Location.LineNumber - 1, (uint) x.Location.CharacterPosition + 5)
					}
				})
				.Union(conditionDefinitions[targetString].Select(x => new Location
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

		bool CheckTraitInheritanceTree(TraitInfo traitInfo, string propertyName, out MemberLocation location)
		{
			MemberLocation? result = null;

			// The property may be a field of the TraitInfo...
			if (traitInfo.TraitPropertyInfos.Any(x => x.PropertyName == propertyName))
			{
				var property = traitInfo.TraitPropertyInfos.FirstOrDefault(x => x.PropertyName == propertyName);
				result = property.Location;
			}
			else
			{
				// ... or it could be inherited.
				foreach (var inheritedType in traitInfo.InheritedTypes)
					if (TryGetTraitInfo(inheritedType, out var inheritedTraitInfo, false))
						if (CheckTraitInheritanceTree(inheritedTraitInfo, propertyName, out var inheritedLocation))
							result = inheritedLocation;
			}

			if (result == null)
			{
				location = default;
				return false;
			}

			location = result.Value;
			return true;
		}
	}
}
