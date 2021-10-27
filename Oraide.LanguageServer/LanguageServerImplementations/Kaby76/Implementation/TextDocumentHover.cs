using System;
using System.Linq;
using LspTypes;
using Newtonsoft.Json.Linq;
using Oraide.LanguageServer.CodeParsers;
using StreamJsonRpc;
using Range = LspTypes.Range;

namespace Oraide.LanguageServer.LanguageServerImplementations.Kaby76.Implementation
{
	public partial class LSPServer
	{
		[JsonRpcMethod(Methods.TextDocumentHoverName)]
		public Hover OnHover(JToken arg)
		{
			lock (_object)
			{
				Hover hover = null;
				try
				{
					if (trace)
					{
						Console.Error.WriteLine("<-- TextDocument-Hover");
						Console.Error.WriteLine(arg.ToString());
					}

					var request = arg.ToObject<TextDocumentPositionParams>();
					if (TryGetHoverInfo(request, out var hoverInfo))
					{
						hover = new Hover
						{
							Contents = new SumType<string, MarkedString, MarkedString[], MarkupContent>(new MarkupContent
							{
								Kind = MarkupKind.PlainText,
								Value = hoverInfo.Content
							}),
							Range = hoverInfo.Range
						};
					}
				}
				catch (Exception)
				{
				}

				return hover;
			}
		}

		private bool TryGetHoverInfo(TextDocumentPositionParams request, out (string Content, Range Range) hoverInfo)
		{
			// var document = CheckDoc(request.TextDocument.Uri);
			var position = request.Position;
			var targetLine = (int)position.Line;
			var targetCharacter = (int)position.Character;
			// var index = new LanguageServer.Module().GetIndex(line, character, document);
			var fileIdentifier = new Uri(Uri.UnescapeDataString(request.TextDocument.Uri)).AbsolutePath;
			var actorDefinitionsInDocument = actorDefinitionsPerFile[fileIdentifier].Values.ToArray();

			foreach (var actorDefinition in actorDefinitionsInDocument)
			{
				Range range;
				if (actorDefinition.Location.LineNumber == targetLine)
				{
					range = new Range
					{
						Start = new Position((uint)actorDefinition.Location.LineNumber, (uint)actorDefinition.Location.CharacterPosition),
						End = new Position((uint)actorDefinition.Location.LineNumber, (uint)actorDefinition.Location.CharacterPosition + 5)
					};

					hoverInfo = (actorDefinition.Name, range);
					return true;
				}

				foreach (var traitDefinition in actorDefinition.Traits)
				{
					if (traitDefinition.Location.LineNumber == targetLine)
					{
						range = new Range
						{
							Start = new Position((uint)traitDefinition.Location.LineNumber, (uint)traitDefinition.Location.CharacterPosition),
							End = new Position((uint)traitDefinition.Location.LineNumber, (uint)traitDefinition.Location.CharacterPosition + 5)
						};

						var hoverContent = traitDefinition.Name;
						if (TryGetTraitInfo(traitDefinition.Name, out var traitInfo))
						{
							hoverContent = $"{traitInfo.TraitName}\n{traitInfo.Location.FilePath}\n{traitInfo.TraitDescription}";
						}

						hoverInfo = (hoverContent, range);
						return true;
					}
				}
			}

			hoverInfo = (null, null);
			return false;
		}

		private bool TryGetTraitInfo(string traitName, out TraitInfo traitInfo)
		{
			if (traitInfos.ContainsKey($"{traitName}Info"))
			{
				traitInfo = traitInfos[$"{traitName}Info"];
				return true;
			}

			traitInfo = default;
			return false;
		}
	}
}
