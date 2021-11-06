using System;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;
using Oraide.LanguageServer.Caching;
using Range = LspTypes.Range;

namespace Oraide.LanguageServer.LanguageServerProtocolHandlers.TextDocument
{
	public class TextDocumentHoverHandler : BaseRpcMessageHandler
	{
		public TextDocumentHoverHandler(SymbolCache symbolCache, OpenFileCache openFileCache)
			: base(symbolCache, openFileCache) { }

		[OraideCustomJsonRpcMethodTag(Methods.TextDocumentHoverName)]
		public Hover Hover(TextDocumentPositionParams positionParams)
		{
			lock (LockObject)
			{
				try
				{
					if (trace)
					{
						Console.Error.WriteLine("<-- TextDocument-Hover");
						Console.Error.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(positionParams));
					}

					if (TryGetTargetNode(positionParams, out var targetNode, out var targetType, out var targetString))
					{
						if (TryGetTargetCodeHoverInfo(targetNode, out var codeHoverInfo))
							return HoverFromHoverInfo(codeHoverInfo.Content, codeHoverInfo.Range);

						if (TryGetTargetYamlHoverInfo(targetNode, out var yamlHoverInfo))
							return HoverFromHoverInfo(yamlHoverInfo.Content, yamlHoverInfo.Range);
					}
				}
				catch (Exception)
				{
				}

				return null;
			}
		}

		private bool TryGetTargetCodeHoverInfo(YamlNode targetNode, out (string Content, Range Range) hoverInfo)
		{
			var traitName = targetNode.Key.Split('@')[0];
			if (!TryGetTraitInfo(traitName, out var traitInfo))
			{
				hoverInfo = (null, null);
				return false;
			}

			hoverInfo = (
				$"{traitInfo.TraitName}\n{traitInfo.Location.FilePath}\n{traitInfo.TraitDescription}",
				new Range
				{
					Start = new Position((uint)traitInfo.Location.LineNumber, (uint)traitInfo.Location.CharacterPosition),
					End = new Position((uint)traitInfo.Location.LineNumber, (uint)traitInfo.Location.CharacterPosition + 5)
				});

			return true;
		}

		private bool TryGetTargetYamlHoverInfo(YamlNode targetNode, out (string Content, Range Range) hoverInfo)
		{
			// TODO:
			hoverInfo = (null, null);
			return false;
		}

		private static Hover HoverFromHoverInfo(string content, Range range)
		{
			return new Hover
			{
				Contents = new SumType<string, MarkedString, MarkedString[], MarkupContent>(new MarkupContent
				{
					Kind = MarkupKind.PlainText,
					Value = content
				}),
				Range = range
			};
		}
	}
}
