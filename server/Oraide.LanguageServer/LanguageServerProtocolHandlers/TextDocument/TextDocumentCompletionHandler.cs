using System;
using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.Core.Entities;
using Oraide.Core.Entities.Csharp;
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
				// Using .First() is not great but we have no way to differentiate between traits of the same name
				// until the server learns the concept of a mod and loaded assemblies.
				Label = x.First().TraitName,
				Kind = CompletionItemKind.Class,
				Detail = "Trait name. Expand for details >",
				Documentation = x.First().TraitDescription,
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
						var traitName = cursorTarget.TargetNode.ParentNode.Key;
						var traitInfoName = $"{traitName}Info";
						var traits = symbolCache.TraitInfos[traitInfoName];

						var allTraits = new List<TraitInfo>();
						allTraits.AddRange(GetInheritedTraitInfos(traits));

						// Getting all traits and then all their properties is not great but we have no way to differentiate between traits of the same name
						// until the server learns the concept of a mod and loaded assemblies.
						return allTraits
							.SelectMany(x => x.TraitPropertyInfos)
							.DistinctBy(y => y.PropertyName)
							.Select(z => new CompletionItem
							{
								Label = z.PropertyName,
								Kind = CompletionItemKind.Property,
								Detail = "Trait property. Expand for details >",
								Documentation = z.Description,
								CommitCharacters = new[] {":"}
							});
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

		// TODO: Go further than one level of inheritance down.
		IEnumerable<TraitInfo> GetInheritedTraitInfos(IEnumerable<TraitInfo> traitInfos)
		{
			foreach (var traitInfo in traitInfos)
			{
				yield return traitInfo;

				foreach (var inheritedTypeName in traitInfo.InheritedTypes)
					if (symbolCache.TraitInfos.Contains(inheritedTypeName))
						foreach (var inheritedTraitInfo in symbolCache.TraitInfos[inheritedTypeName])
							yield return inheritedTraitInfo;
			}
		}
	}
}
