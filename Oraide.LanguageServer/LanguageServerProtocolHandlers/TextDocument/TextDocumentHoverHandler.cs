using System;
using System.Linq;
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

					if (TryGetCursorTarget(positionParams, out var target))
					{
						if (TryGetTargetCodeHoverInfo(target, out var codeHoverInfo))
							return HoverFromHoverInfo(codeHoverInfo.Content, codeHoverInfo.Range);

						if (TryGetTargetYamlHoverInfo(target.TargetNode, out var yamlHoverInfo))
							return HoverFromHoverInfo(yamlHoverInfo.Content, yamlHoverInfo.Range);
					}
				}
				catch (Exception)
				{
				}

				return null;
			}
		}

		private bool TryGetTargetCodeHoverInfo(CursorTarget target, out (string Content, Range Range) hoverInfo)
		{
			if (!TryGetCodeMemberLocation(target.TargetNode, target.TargetString, out var traitInfo, out _))
			{
				hoverInfo = (null, null);
				return false;
			}

			string content;
			if (traitInfo.TraitName == target.TargetString)
			{
				content = $"{traitInfo.TraitInfoName}\n{traitInfo.Location.FilePath}\n{traitInfo.TraitDescription}";
			}
			else
			{
				var prop = traitInfo.TraitPropertyInfos.FirstOrDefault(x => x.PropertyName == target.TargetString);
				content = $"{prop.PropertyName}\n{prop.Description}";
			}

			if (string.IsNullOrWhiteSpace(content))
			{
				hoverInfo = (null, null);
				return false;
			}

			hoverInfo = (
				content,
				new Range
				{
					Start = new Position((uint)target.TargetStart.LineNumber, (uint)target.TargetStart.CharacterPosition),
					End = new Position((uint)target.TargetEnd.LineNumber, (uint)target.TargetEnd.CharacterPosition)
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
