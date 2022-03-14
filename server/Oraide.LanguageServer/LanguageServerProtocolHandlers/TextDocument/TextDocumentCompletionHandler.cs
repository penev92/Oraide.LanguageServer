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
		string modId;
		IEnumerable<CompletionItem> traitNames;
		IEnumerable<CompletionItem> actorNames;
		IEnumerable<CompletionItem> weaponNames;
		IEnumerable<CompletionItem> conditionNames;
		IEnumerable<CompletionItem> cursorNames;
		WeaponInfo weaponInfo;

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
						Console.Error.WriteLine("<-- TextDocument-Completion");

					var completionItems = HandlePositionalRequest(completionParams) as IEnumerable<CompletionItem>;
					return new CompletionList
					{
						IsIncomplete = false,
						Items = completionItems?.ToArray()
					};

				}
				catch (Exception e)
				{
					Console.Error.WriteLine("EXCEPTION!!!");
					Console.Error.WriteLine(e.ToString());
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
			else if (filePath.Contains("cursor")) // TODO: These checks are getting ridiculous.
				fileType = FileType.Cursors;

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

		protected override void Initialize(CursorTarget cursorTarget)
		{
			modId = cursorTarget.ModId;

			// Using .First() is not great but we have no way to differentiate between traits of the same name
			// until the server learns the concept of a mod and loaded assemblies.
			traitNames = symbolCache[modId].TraitInfos.Select(x => x.First().ToCompletionItem());
			actorNames = symbolCache[modId].ActorDefinitions.Select(x => x.First().ToCompletionItem());
			weaponNames = symbolCache[modId].WeaponDefinitions.Select(x => x.First().ToCompletionItem());
			conditionNames = symbolCache[modId].ConditionDefinitions.Select(x => x.First().ToCompletionItem());
			cursorNames = symbolCache[modId].CursorDefinitions.Select(x => x.First().ToCompletionItem());

			weaponInfo = symbolCache[modId].WeaponInfo;
		}

		#region CursorTarget handlers

		protected override IEnumerable<CompletionItem> HandleRulesKey(CursorTarget cursorTarget)
		{
			switch (cursorTarget.TargetNodeIndentation)
			{
				case 0:
					// Get only actor definitions. Presumably for reference and for overriding purposes.
					return actorNames;

				case 1:
					// Get only traits (and "Inherits").
					return traitNames.Append(inheritsCompletionItem);

				case 2:
				{
					// Get only trait properties.
					var traitName = cursorTarget.TargetNode.ParentNode.Key.Split('@')[0];
					var traitInfoName = $"{traitName}Info";
					var presentProperties = cursorTarget.TargetNode.ParentNode.ChildNodes.Select(x => x.Key).ToHashSet();

					// Getting all traits and then all their properties is not great but we have no way to differentiate between traits of the same name
					// until the server learns the concept of a mod and loaded assemblies.
					return symbolCache[modId].TraitInfos[traitInfoName]
						.SelectMany(x => x.TraitPropertyInfos)
						.DistinctBy(y => y.Name)
						.Where(x => !presentProperties.Contains(x.Name))
						.Select(z => z.ToCompletionItem());
				}

				default:
					return Enumerable.Empty<CompletionItem>();
			}
		}

		protected override IEnumerable<CompletionItem> HandleRulesValue(CursorTarget cursorTarget)
		{
			switch (cursorTarget.TargetNodeIndentation)
			{
				case 0:
					return Enumerable.Empty<CompletionItem>();

				case 1:
				{
					// Get actor definitions (for inheriting).
					if (cursorTarget.TargetNode.Key == "Inherits" || cursorTarget.TargetNode.Key.StartsWith("Inherits@"))
						return actorNames;

					return Enumerable.Empty<CompletionItem>();
				}

				case 2:
				{
					var traitName = cursorTarget.TargetNode.ParentNode.Key.Split('@')[0];
					var traitInfoName = $"{traitName}Info";

					// Using .First() is not great but we have no way to differentiate between traits of the same name
					// until the server learns the concept of a mod and loaded assemblies.
					var traitInfo = symbolCache[modId].TraitInfos[traitInfoName].First();
					var fieldInfo = traitInfo.TraitPropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.Key);

					if (fieldInfo.OtherAttributes.Any(x => x.Name == "ActorReference"))
						return actorNames;

					if (fieldInfo.OtherAttributes.Any(x => x.Name == "WeaponReference"))
						return weaponNames;

					if (fieldInfo.OtherAttributes.Any(x => x.Name == "GrantedConditionReference"))
						return conditionNames;

					if (fieldInfo.OtherAttributes.Any(x => x.Name == "ConsumedConditionReference"))
						return conditionNames;

					if (fieldInfo.OtherAttributes.Any(x => x.Name == "CursorReference"))
						return cursorNames;

					return Enumerable.Empty<CompletionItem>();
				}

				default:
					return Enumerable.Empty<CompletionItem>();
			}
		}

		protected override IEnumerable<CompletionItem> HandleWeaponKey(CursorTarget cursorTarget)
		{
			switch (cursorTarget.TargetNodeIndentation)
			{
				case 0:
					// Get only weapon definitions. Presumably for reference and for overriding purposes.
					return weaponNames;

				case 1:
					// Get only WeaponInfo fields (and "Inherits" and "Warhead").
					return weaponInfo.WeaponPropertyInfos
						.Select(x => x.ToCompletionItem())
						.Append(warheadCompletionItem)
						.Append(inheritsCompletionItem);

				case 2:
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

					return Enumerable.Empty<CompletionItem>();
				}

				default:
					return Enumerable.Empty<CompletionItem>();
			}
		}

		protected override IEnumerable<CompletionItem> HandleWeaponValue(CursorTarget cursorTarget)
		{
			switch (cursorTarget.TargetNodeIndentation)
			{
				case 0:
					return Enumerable.Empty<CompletionItem>();

				case 1:
				{
					var nodeKey = cursorTarget.TargetNode.Key;

					// Get weapon definitions (for inheriting).
					if (nodeKey == "Inherits" || nodeKey.StartsWith("Inherits@"))
						return weaponNames;

					if (nodeKey == "Projectile")
						return weaponInfo.ProjectileInfos.Select(x => x.ToCompletionItem("Type implementing IProjectileInfo"));

					if (nodeKey == "Warhead" || nodeKey.StartsWith("Warhead@"))
						return weaponInfo.WarheadInfos.Select(x => x.ToCompletionItem("Type implementing IWarhead"));

					return Enumerable.Empty<CompletionItem>();
				}

				case 2:
					return Enumerable.Empty<CompletionItem>();

				default:
					return Enumerable.Empty<CompletionItem>();
			}
		}

		protected override IEnumerable<CompletionItem> HandleCursorsValue(CursorTarget cursorTarget)
		{
			// TODO: Return palette information when we have support for palettes.
			return null;
		}

		#endregion
	}
}
