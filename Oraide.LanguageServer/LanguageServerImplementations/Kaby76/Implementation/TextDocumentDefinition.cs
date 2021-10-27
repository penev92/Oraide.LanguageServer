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
					if (TryGetDefinitionLocations(request, out var definitionLocations))
						return definitionLocations;
				}
				catch (Exception)
				{
				}

				return Enumerable.Empty<Location>();
			}
		}

		private bool TryGetDefinitionLocations(TextDocumentPositionParams request, out IEnumerable<Location> definitionLocations)
		{
			var position = request.Position;
			var targetLine = (int)position.Line;
			var targetCharacter = (int)position.Character;
			// var index = new LanguageServer.Module().GetIndex(line, character, document);
			var fileIdentifier = new Uri(Uri.UnescapeDataString(request.TextDocument.Uri)).AbsolutePath;
			var actorDefinitionsInDocument = actorDefinitionsPerFile[fileIdentifier].Values.ToArray();

			Range range;
			var traitName = "";
			foreach (var actorDefinition in actorDefinitionsInDocument)
			{
				foreach (var traitDefinition in actorDefinition.Traits)
				{
					if (traitDefinition.Location.LineNumber == targetLine)
					{
						range = new Range
						{
							Start = new Position((uint)traitDefinition.Location.LineNumber, (uint)traitDefinition.Location.CharacterPosition),
							End = new Position((uint)traitDefinition.Location.LineNumber, (uint)traitDefinition.Location.CharacterPosition + 5)
						};

						traitName = traitDefinition.Name;
					}
				}
			}

			var traitInfoName = $"{traitName}Info";
			if (!traitInfos.ContainsKey(traitInfoName))
			{
				definitionLocations = Enumerable.Empty<Location>();
				return false;
			}

			var traitInfo = traitInfos[traitInfoName];
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
	}
}
