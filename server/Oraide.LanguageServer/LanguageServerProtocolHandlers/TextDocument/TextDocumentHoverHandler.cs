using System;
using System.Linq;
using System.Text.RegularExpressions;
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
						if (target.TargetType == "key"
						    && (target.TargetNodeIndentation == 1 || target.TargetNodeIndentation == 2)
						    && TryGetTargetCodeHoverInfo(target, out var codeHoverInfo))
							return HoverFromHoverInfo(codeHoverInfo.Content, codeHoverInfo.Range);

						if ((target.TargetType == "value" || target.TargetNodeIndentation == 0)
						    && TryGetTargetYamlHoverInfo(target, out var yamlHoverInfo))
							return HoverFromHoverInfo(yamlHoverInfo.Content, yamlHoverInfo.Range);

						if (TryGetTargetValueHoverInfo(target, out var valueHoverInfo))
							return HoverFromHoverInfo(valueHoverInfo.Content, valueHoverInfo.Range);
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
				content = "```csharp\n" +
				          $"class {traitInfo.TraitInfoName}" +
				          $"\n```\n" +
						  $"{traitInfo.TraitDescription}\n\n" +
				          "https://docs.openra.net/en/latest/release/traits/#" + $"{traitInfo.TraitName.ToLower()}";
			}
			else
			{
				var prop = traitInfo.TraitPropertyInfos.FirstOrDefault(x => x.PropertyName == target.TargetString);
				content = "```csharp\n" +
				          $"{prop.PropertyName} ({prop.PropertyType})" +
				          $"\n```\n" +
						  $"{prop.Description}\n\nDefault value: {prop.DefaultValue}";
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

		private bool TryGetTargetYamlHoverInfo(CursorTarget target, out (string Content, Range Range) hoverInfo)
		{
			if (symbolCache.ActorDefinitionsPerMod[target.ModId].Any(x => x.Key == target.TargetString))
			{
				hoverInfo = (
					$"```csharp\nActor \"{target.TargetString}\"\n```", new Range
					{
						Start = new Position((uint)target.TargetStart.LineNumber, (uint)target.TargetStart.CharacterPosition),
						End = new Position((uint)target.TargetEnd.LineNumber, (uint)target.TargetEnd.CharacterPosition)
					});

				return true;
			}

			if (symbolCache.WeaponDefinitionsPerMod[target.ModId].Any(x => x.Key == target.TargetString))
			{
				hoverInfo = (
					$"```csharp\nWeapon \"{target.TargetString}\"\n```", new Range
					{
						Start = new Position((uint)target.TargetStart.LineNumber, (uint)target.TargetStart.CharacterPosition),
						End = new Position((uint)target.TargetEnd.LineNumber, (uint)target.TargetEnd.CharacterPosition)
					});

				return true;
			}

			if (symbolCache.ConditionDefinitionsPerMod[target.ModId].Any(x => x.Key == target.TargetString))
			{
				hoverInfo = (
					$"```csharp\nCondition \"{target.TargetString}\"\n```", new Range
					{
						Start = new Position((uint)target.TargetStart.LineNumber, (uint)target.TargetStart.CharacterPosition),
						End = new Position((uint)target.TargetEnd.LineNumber, (uint)target.TargetEnd.CharacterPosition)
					});

				return true;
			}

			hoverInfo = (null, null);
			return false;
		}

		private bool TryGetTargetValueHoverInfo(CursorTarget target, out (string Content, Range Range) hoverInfo)
		{
			if (target.TargetType == "value")
			{
				if (Regex.IsMatch(target.TargetString, "[0-9]+c[0-9]+", RegexOptions.Compiled))
				{
					var parts = target.TargetString.Split('c');
					hoverInfo = ($"Range in world distance units equal to {parts[0]} cells and {parts[1]} distance units (where 1 cell has 1024 units)", new Range
					{
						Start = new Position((uint)target.TargetStart.LineNumber, (uint)target.TargetStart.CharacterPosition),
						End = new Position((uint)target.TargetEnd.LineNumber, (uint)target.TargetEnd.CharacterPosition)
					});

					return true;
				}
			}

			hoverInfo = (null, null);
			return false;
		}

		private static Hover HoverFromHoverInfo(string content, Range range)
		{
			return new Hover
			{
				Contents = new SumType<string, MarkedString, MarkedString[], MarkupContent>(new MarkupContent
				{
					Kind = MarkupKind.Markdown,
					Value = content
				}),
				Range = range
			};
		}
	}
}
