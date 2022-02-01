using System;
using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.Core.Entities;
using Oraide.Core.Entities.Csharp;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;
using Oraide.LanguageServer.Caching;
using Oraide.LanguageServer.Extensions;

namespace Oraide.LanguageServer.LanguageServerProtocolHandlers.TextDocument
{
	public class TextDocumentCompletionHandler : BaseRpcMessageHandler
	{
		readonly CompletionItem inheritsCompletionItem = new ()
		{
			Label = "Inherits",
			Kind = CompletionItemKind.Constructor,
			Detail = "Allows rule inheritance.",
			CommitCharacters = new[] { ":" }
		};

		readonly CompletionItem warheadCompletionItem = new ()
		{
			Label = "Warhead",
			Kind = CompletionItemKind.Constructor,
			Detail = "A warhead to be used by this weapon. You can list several of these.",
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
			var modId = cursorTarget.ModId;

			// Using .First() is not great but we have no way to differentiate between traits of the same name
			// until the server learns the concept of a mod and loaded assemblies.
			var traitNames = symbolCache[modId].TraitInfos.Select(x => x.First().ToCompletionItem());
			var actorNames = symbolCache[modId].ActorDefinitions.Select(x => x.First().ToCompletionItem());
			var weaponNames = symbolCache[modId].WeaponDefinitions.Select(x => x.First().ToCompletionItem());
			var conditionNames = symbolCache[modId].ConditionDefinitions.Select(x => x.First().ToCompletionItem());

			if (cursorTarget.FileType == FileType.Rules)
			{
				if (cursorTarget.TargetNodeIndentation == 0)
				{
					// Get only actor definitions. Presumably for reference and for overriding purposes.
					return actorNames;
				}

				if (cursorTarget.TargetNodeIndentation == 1)
				{
					if (cursorTarget.TargetType == "key")
					{
						// Get only traits (and "Inherits").
						return traitNames.Append(inheritsCompletionItem);
					}

					if (cursorTarget.TargetType == "value")
					{
						// Get actor definitions (for inheriting).
						return actorNames;
					}
				}
				else if (cursorTarget.TargetNodeIndentation == 2)
				{
					if (cursorTarget.TargetType == "key")
					{
						// Get only trait properties.
						var traitName = cursorTarget.TargetNode.ParentNode.Key.Split('@')[0];
						var traitInfoName = $"{traitName}Info";
						var traits = symbolCache[cursorTarget.ModId].TraitInfos[traitInfoName];
						var presentProperties = cursorTarget.TargetNode.ParentNode.ChildNodes.Select(x => x.Key).ToHashSet();

						var inheritedTraits = new List<TraitInfo>();
						inheritedTraits.AddRange(GetInheritedTraitInfos(symbolCache[cursorTarget.ModId], traits));

						// Getting all traits and then all their properties is not great but we have no way to differentiate between traits of the same name
						// until the server learns the concept of a mod and loaded assemblies.
						return inheritedTraits
							.SelectMany(x => x.TraitPropertyInfos)
							.DistinctBy(y => y.PropertyName)
							.Where(x => !presentProperties.Contains(x.PropertyName))
							.Select(z => z.ToCompletionItem());
					}

					if (cursorTarget.TargetType == "value")
					{
						// This would likely be a TraitInfo property's value, so at this point it's anyone's guess. Probably skip until we can give type-specific suggestions.
						return traitNames.Union(actorNames).Union(weaponNames).Union(conditionNames);
					}
				}
			}
			else if (cursorTarget.FileType == FileType.Weapons)
			{
				var weaponInfo = symbolCache[modId].WeaponInfo;

				// Get only weapon definitions. Presumably for reference and for overriding purposes.
				if (cursorTarget.TargetNodeIndentation == 0)
					return weaponNames;

				if (cursorTarget.TargetNodeIndentation == 1)
				{
					if (cursorTarget.TargetType == "key")
					{
						// Get only WeaponInfo fields (and "Inherits" and "Warhead").
						return weaponInfo.WeaponPropertyInfos
							.Select(x => x.ToCompletionItem())
							.Append(warheadCompletionItem)
							.Append(inheritsCompletionItem);
					}

					if (cursorTarget.TargetType == "value")
					{
						var nodeKey = cursorTarget.TargetNode.Key;

						// Get weapon definitions (for inheriting).
						if (nodeKey == "Inherits" || nodeKey.StartsWith("Inherits@"))
							return weaponNames;

						if (nodeKey == "Projectile")
							return weaponInfo.ProjectileInfos.Select(x => x.ToCompletionItem("Type implementing IProjectileInfo"));

						if (nodeKey == "Warhead" || nodeKey.StartsWith("Warhead@"))
							return weaponInfo.WarheadInfos.Select(x => x.ToCompletionItem("Type implementing IWarhead"));
					}
				}

				if (cursorTarget.TargetNodeIndentation == 2)
				{
					var parentNode = cursorTarget.TargetNode.ParentNode;
					if (parentNode.Key == "Projectile")
					{
						var projectile = weaponInfo.ProjectileInfos.FirstOrDefault(x => x.Name == parentNode.Value);
						if (projectile.Name != null)
							return projectile.PropertyInfos.Select(x => x.ToCompletionItem());
					}
					else if (parentNode.Key == "Warhead" || parentNode.Key.StartsWith("Warhead@"))
					{
						var warhead = weaponInfo.WarheadInfos.FirstOrDefault(x => x.Name == parentNode.Value);
						if (warhead.Name != null)
							return warhead.PropertyInfos.Select(x => x.ToCompletionItem());
					}
				}
			}

			return Enumerable.Empty<CompletionItem>();
		}

		// TODO: Go further than one level of inheritance down.
		IEnumerable<TraitInfo> GetInheritedTraitInfos(ModSymbols modSymbols, IEnumerable<TraitInfo> traitInfos)
		{
			foreach (var traitInfo in traitInfos)
			{
				yield return traitInfo;

				foreach (var inheritedTypeName in traitInfo.InheritedTypes)
					if (modSymbols.TraitInfos.Contains(inheritedTypeName))
						foreach (var inheritedTraitInfo in modSymbols.TraitInfos[inheritedTypeName])
							yield return inheritedTraitInfo;
			}
		}
	}
}
