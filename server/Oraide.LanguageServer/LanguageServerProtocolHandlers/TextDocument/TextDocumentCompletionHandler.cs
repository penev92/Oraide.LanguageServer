using System;
using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.Core.Entities;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;
using Oraide.LanguageServer.Caching;

namespace Oraide.LanguageServer.LanguageServerProtocolHandlers.TextDocument
{
	public class TextDocumentCompletionHandler : BaseRpcMessageHandler
	{
		readonly CompletionItem inherits = new ()
		{
			Label = "Inherits",
			Kind = CompletionItemKind.Constructor,
			Detail = "Allows rule inheritance.",
			CommitCharacters = new[] { ":" }
		};

		public TextDocumentCompletionHandler(SymbolCache symbolCache, OpenFileCache openFileCache)
			: base(symbolCache, openFileCache) { }

		[OraideCustomJsonRpcMethodTag(Methods.TextDocumentCompletionName)]
		public CompletionList CompletionTextDocument(CompletionParams completionParams)
		{
			lock (LockObject)
			{
				try
				{
					if (trace)
					{
						Console.Error.WriteLine("<-- TextDocument-Completion");
						Console.Error.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(completionParams));

						TryGetCursorTarget(completionParams, out var cursorTarget);
						return new CompletionList
						{
							IsIncomplete = false,
							Items = GetCompletionItems(cursorTarget).ToArray()
						};
					}

				}
				catch (Exception e)
				{
				}

				return null;
			}
		}

		protected override bool TryGetCursorTarget(TextDocumentPositionParams positionParams, out CursorTarget target)
		{
			var filePath = positionParams.TextDocument.Uri;
			var targetLineIndex = (int)positionParams.Position.Line;
			var targetCharacterIndex = (int)positionParams.Position.Character;

			// Determine file type.
			var fileType = FileType.Unknown;
			if (filePath.Contains("/rules/") || (filePath.Contains("/maps/") && !filePath.EndsWith("map.yaml")))
				fileType = FileType.Rules;
			else if (filePath.Contains("/weapons/"))
				fileType = FileType.Weapons;

			if (!openFileCache.ContainsFile(filePath))
			{
				target = default;
				return false;
			}

			var (fileLines, fileNodes) = openFileCache[filePath];

			var targetLine = fileLines[targetLineIndex];
			var pre = targetLine.Substring(0, targetCharacterIndex);

			var targetNode = fileNodes[targetLineIndex];

			string sourceString;
			string targetType;

			if (pre.Contains(':'))
			{
				targetType = "value";
				sourceString = targetNode.Value;
			}
			else
			{
				if (pre.Contains('@'))
				{
					targetType = "keyIdentifier";
					sourceString = string.Empty;
				}
				else
				{
					targetType = "key";
					sourceString = targetNode.Key ?? string.Empty;
				}
			}

			TryGetModId(positionParams.TextDocument.Uri, out var modId);
			TryGetTargetStringIndentation(targetNode, out var indentation);
			target = new CursorTarget(modId, fileType, targetNode, targetType, sourceString,
				new MemberLocation(filePath, targetLineIndex, targetCharacterIndex),
				new MemberLocation(filePath, targetLineIndex, targetCharacterIndex), indentation);

			return true;
		}

		IEnumerable<CompletionItem> GetCompletionItems(CursorTarget cursorTarget)
		{
			var traitNames = symbolCache.TraitInfos.Select(x => new CompletionItem
			{
				Label = x.Value.TraitName,
				Kind = CompletionItemKind.Class,
				Detail = "Trait name. Expand for details >",
				Documentation = x.Value.TraitDescription,
				CommitCharacters = new[] { ":" }
			});

			var traitProperties = symbolCache.TraitInfos.SelectMany(x => x.Value.TraitPropertyInfos)
				.Select(x => new CompletionItem
				{
					Label = x.PropertyName,
					Kind = CompletionItemKind.Property,
					Detail = "Trait property. Expand for details >",
					Documentation = x.Description,
					CommitCharacters = new[] { ":" }
				});

			var actorNames = symbolCache.ActorDefinitionsPerMod[cursorTarget.ModId].Select(x => new CompletionItem
			{
				Label = x.Key,
				Kind = CompletionItemKind.Unit,
				Detail = "Actor name",
				CommitCharacters = new[] { ":" }
			});

			var weaponNames = symbolCache.WeaponDefinitionsPerMod[cursorTarget.ModId].Select(x => new CompletionItem
			{
				Label = x.Key,
				Kind = CompletionItemKind.Unit,
				Detail = "Weapon name",
				CommitCharacters = new[] { ":" }
			});

			var conditionNames = symbolCache.ConditionDefinitionsPerMod[cursorTarget.ModId].Select(x => new CompletionItem
			{
				Label = x.Key,
				Kind = CompletionItemKind.Value,
				Detail = "Condition",
				Documentation = "Conditions are arbitrary user-defined strings that are used across multiple actors/weapons/traits."
			});

			if (cursorTarget.FileType == FileType.Rules)
			{
				if (cursorTarget.TargetNodeIndentation == 0)
				{
					// Get only actors. Presumably for reference and for overriding purposes.
					return actorNames;
				}

				if (cursorTarget.TargetNodeIndentation == 1)
				{
					if (cursorTarget.TargetType == "key")
					{
						// Get only traits (and "Inherits").
						return traitNames.Append(inherits);
					}

					if (cursorTarget.TargetType == "value")
					{
						// Get actors (for inheriting).
						return actorNames;
					}
				}
				else if (cursorTarget.TargetNodeIndentation == 2)
				{
					if (cursorTarget.TargetType == "key")
					{
						// Get only trait properties.
						return traitProperties;
					}

					if (cursorTarget.TargetType == "value")
					{
						// This would likely be a TraitInfo property's value, so at this point it's anyone's guess. Probably skip until we can give type-specific suggestions.
						return traitNames.Union(actorNames).Union(weaponNames).Union(conditionNames);
					}
				}
			}

			return Enumerable.Empty<CompletionItem>();
		}
	}
}
