﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LspTypes;
using Oraide.Core;
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
		IEnumerable<CompletionItem> paletteNames;
		IEnumerable<CompletionItem> spriteSequenceImageNames;
		OldWeaponInfo weaponInfo;

		readonly CompletionItem inheritsCompletionItem = new ()
		{
			Label = "Inherits",
			Kind = CompletionItemKind.Constructor,
			Detail = "Allows rule inheritance.",
			CommitCharacters = new[] { ":" }
		};

		readonly CompletionItem defaultsCompletionItem = new ()
		{
			Label = "Defaults",
			Kind = CompletionItemKind.Constructor,
			Detail = "Sets default values for all sequences of this image.",
			CommitCharacters = new[] { ":" }
		};

		readonly CompletionItem warheadCompletionItem = new ()
		{
			Label = "Warhead",
			Kind = CompletionItemKind.Constructor,
			Detail = "A warhead to be used by this weapon. You can list several of these.",
			CommitCharacters = new[] { ":" }
		};

		readonly CompletionItem trueCompletionItem = new ()
		{
			Label = "true",
			Kind = CompletionItemKind.Value,
			Detail = "A boolean value."
		};

		readonly CompletionItem falseCompletionItem = new ()
		{
			Label = "false",
			Kind = CompletionItemKind.Value,
			Detail = "A boolean value."
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
			// HACK HACK HACK!!!
			// For whatever reason we receive the file URI borked - looks to be encoded for JSON, but the deserialization doesn't fix it.
			// No idea if this is an issue with VSCode or the LSP library used as there are currently no clients for other text editors.
			var incomingFileUriString = OpenRaFolderUtils.NormalizeFileUriString(positionParams.TextDocument.Uri);

			TryGetModId(incomingFileUriString, out var modId);
			var fileUri = new Uri(incomingFileUriString);
			var targetLineIndex = (int)positionParams.Position.Line;
			var targetCharacterIndex = (int)positionParams.Position.Character;

			// Determine file type.
			var modManifest = symbolCache[modId].ModManifest;
			var filePath = fileUri.AbsolutePath;
			var fileName = filePath.Split($"mods/{modId}/")[1];
			var fileReference = $"{modId}|{fileName}";

			var fileType = FileType.Unknown;
			if (modManifest.RulesFiles.Contains(fileReference))
				fileType = FileType.Rules;
			else if (modManifest.WeaponsFiles.Contains(fileReference))
				fileType = FileType.Weapons;
			else if (modManifest.CursorsFiles.Contains(fileReference))
				fileType = FileType.Cursors;
			else if (modManifest.SpriteSequences.Contains(fileReference))
				fileType = FileType.SpriteSequences;
			else if (Path.GetFileName(filePath) == "map.yaml" && symbolCache[modId].Maps.Any(x => x.MapFolder == Path.GetDirectoryName(filePath)))
				fileType = FileType.MapFile;
			else if (symbolCache[modId].Maps.Any(x => x.RulesFiles.Contains(fileReference)))
				fileType = FileType.MapRules;
			else if (symbolCache[modId].Maps.Any(x => x.WeaponsFiles.Contains(fileReference)))
				fileType = FileType.MapWeapons;
			else if (symbolCache[modId].Maps.Any(x => x.SpriteSequenceFiles.Contains(fileReference)))
				fileType = FileType.MapSpriteSequences;

			if (!openFileCache.ContainsFile(fileUri.AbsoluteUri))
			{
				target = default;
				return false;
			}

			var (fileNodes, flattenedNodes, fileLines) = openFileCache[fileUri.AbsoluteUri];

			var targetLine = fileLines[targetLineIndex];

			// If the target line is a comment we probably don't care about it - bail out early.
			if (Regex.IsMatch(targetLine, "^\\s#"))
			{
				target = default;
				return false;
			}

			var pre = targetLine.Substring(0, targetCharacterIndex);

			var targetNode = flattenedNodes[targetLineIndex];

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

			TryGetTargetStringIndentation(targetNode, out var indentation);
			target = new CursorTarget(modId, fileType, fileReference, targetNode, targetType, sourceString,
				new MemberLocation(fileUri, targetLineIndex, targetCharacterIndex),
				new MemberLocation(fileUri, targetLineIndex, targetCharacterIndex), indentation);

			return true;
		}

		protected override void Initialize(CursorTarget cursorTarget)
		{
			modId = cursorTarget.ModId;

			// TODO: Don't map everything to CompletionItems here! Defer that until we know what we need, then only map that (like in DefinitionHandler).
			// Using .First() is not great but we have no way to differentiate between traits of the same name
			// until the server learns the concept of a mod and loaded assemblies.
			traitNames = symbolCache[modId].CodeSymbols.TraitInfos.Where(x => !x.First().IsAbstract).Select(x => x.First().ToCompletionItem());
			actorNames = symbolCache[modId].ModSymbols.ActorDefinitions.Select(x => x.First().ToCompletionItem());
			weaponNames = symbolCache[modId].ModSymbols.WeaponDefinitions.Select(x => x.First().ToCompletionItem());
			conditionNames = symbolCache[modId].ModSymbols.ConditionDefinitions.Select(x => x.First().ToCompletionItem());
			cursorNames = symbolCache[modId].ModSymbols.CursorDefinitions.Select(x => x.First().ToCompletionItem());
			paletteNames = symbolCache[modId].ModSymbols.PaletteDefinitions.Select(x => x.First().ToCompletionItem());
			spriteSequenceImageNames = symbolCache[modId].ModSymbols.SpriteSequenceImageDefinitions.Select(x => x.First().ToCompletionItem());

			weaponInfo = symbolCache[modId].CodeSymbols.WeaponInfo;
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
					return symbolCache[modId].CodeSymbols.TraitInfos[traitInfoName]
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
					{
						if (cursorTarget.FileType == FileType.MapRules)
						{
							var mapReference = symbolCache[cursorTarget.ModId].Maps
								.FirstOrDefault(x => x.RulesFiles.Contains(cursorTarget.FileReference));

							if (mapReference.MapReference != null && symbolCache.Maps.TryGetValue(mapReference.MapReference, out var mapSymbols))
								return actorNames.Union(mapSymbols.ActorDefinitions.Select(x => x.First().ToCompletionItem()));
						}

						return actorNames;
					}

					return Enumerable.Empty<CompletionItem>();
				}

				case 2:
				{
					var traitName = cursorTarget.TargetNode.ParentNode.Key.Split('@')[0];
					var traitInfoName = $"{traitName}Info";

					// Using .First() is not great but we have no way to differentiate between traits of the same name
					// until the server learns the concept of a mod and loaded assemblies.
					var traitInfo = symbolCache[modId].CodeSymbols.TraitInfos[traitInfoName].First();
					var fieldInfo = traitInfo.TraitPropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.Key);

					var tempActorNames = actorNames;
					var tempWeaponNames = weaponNames;
					var tempConditionNames = conditionNames;
					var tempCursorNames = cursorNames;

					MapManifest mapManifest = default;
					if (cursorTarget.FileType == FileType.MapRules)
					{
						mapManifest = symbolCache[cursorTarget.ModId].Maps
							.FirstOrDefault(x => x.RulesFiles.Contains(cursorTarget.FileReference));

						if (mapManifest.MapReference != null && symbolCache.Maps.TryGetValue(mapManifest.MapReference, out var mapSymbols))
						{
							// TODO: Don't map to everything CompletionItems here! Defer that until we know what we need, then only map that (like in DefinitionHandler).
							tempActorNames = tempActorNames.Union(mapSymbols.ActorDefinitions.Select(x => x.First().ToCompletionItem()));
							tempWeaponNames = tempWeaponNames.Union(mapSymbols.WeaponDefinitions.Select(x => x.First().ToCompletionItem()));
							tempConditionNames = tempConditionNames.Union(mapSymbols.ConditionDefinitions.Select(x => x.First().ToCompletionItem()));
							paletteNames = paletteNames.Union(mapSymbols.PaletteDefinitions.Select(x => x.First().ToCompletionItem()));
							spriteSequenceImageNames = spriteSequenceImageNames.Union(
								mapSymbols.SpriteSequenceImageDefinitions.Select(x => x.First().ToCompletionItem()));
						}
					}

					if (fieldInfo.OtherAttributes.Any(x => x.Name == "ActorReference"))
						return tempActorNames;

					if (fieldInfo.OtherAttributes.Any(x => x.Name == "WeaponReference"))
						return tempWeaponNames;

					if (fieldInfo.OtherAttributes.Any(x => x.Name == "GrantedConditionReference"))
						return tempConditionNames;

					if (fieldInfo.OtherAttributes.Any(x => x.Name == "ConsumedConditionReference"))
						return tempConditionNames;

					if (fieldInfo.OtherAttributes.Any(x => x.Name == "CursorReference"))
						return tempCursorNames;

					if (fieldInfo.OtherAttributes.Any(x => x.Name == "PaletteReference"))
						return paletteNames.Where(x => !string.IsNullOrEmpty(x.Label));

					// Pretend there is such a thing as a "SequenceImageReferenceAttribute" until we add it in OpenRA one day.
					// NOTE: This will improve if/when we add the attribute.
					if (traitInfo.TraitPropertyInfos.Any(x => x.OtherAttributes.Any(y => y.Name == "SequenceReference"
						    && y.Value != null
						    && (y.Value.Contains(',') ? y.Value.Substring(0, y.Value.IndexOf(',')) == fieldInfo.Name : y.Value == fieldInfo.Name))))
					{
						return spriteSequenceImageNames;
					}

					if (fieldInfo.OtherAttributes.Any(x => x.Name == "SequenceReference"))
					{
						// Resolve sequence image inheritance so we can show all inherited sequences.
						var imageName = ResolveSpriteSequenceImageNameForRules(cursorTarget, fieldInfo, mapManifest);
						var sequences = GetSpriteSequencesForImage(cursorTarget.ModId, imageName, mapManifest);
						return sequences.Select(x => x.ToCompletionItem());
					}

					// Try to check if this is an enum type field.
					var enumInfo = symbolCache[cursorTarget.ModId].CodeSymbols.EnumInfos.FirstOrDefault(x => x.Key == fieldInfo.InternalType);
					if (enumInfo != null)
					{
						return enumInfo.FirstOrDefault().Values.Select(x => new CompletionItem
						{
							Label = x,
							Kind = CompletionItemKind.EnumMember,
							Detail = "Enum type value",
							Documentation = $"{enumInfo.Key}.{x}"
						});
					}

					if (fieldInfo.InternalType == "bool")
					{
						return new[] { trueCompletionItem, falseCompletionItem };
					}

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
					{
						if (cursorTarget.FileType == FileType.MapWeapons)
						{
							var mapReference = symbolCache[cursorTarget.ModId].Maps
								.FirstOrDefault(x => x.WeaponsFiles.Contains(cursorTarget.FileReference));

							if (mapReference.MapReference != null && symbolCache.Maps.TryGetValue(mapReference.MapReference, out var mapSymbols))
								return weaponNames.Union(mapSymbols.WeaponDefinitions.Select(x => x.First().ToCompletionItem()));
						}

						return weaponNames;
					}

					if (nodeKey == "Projectile")
						return weaponInfo.ProjectileInfos.Where(x => !x.IsAbstract).Select(x => x.ToCompletionItem("Type implementing IProjectileInfo"));

					if (nodeKey == "Warhead" || nodeKey.StartsWith("Warhead@"))
						return weaponInfo.WarheadInfos.Where(x => !x.IsAbstract).Select(x => x.ToCompletionItem("Type implementing IWarhead"));

					return Enumerable.Empty<CompletionItem>();
				}

				case 2:
				{
					ClassFieldInfo fieldInfo = default;
					var classFieldInfos = Array.Empty<ClassFieldInfo>();
					var parentNode = cursorTarget.TargetNode.ParentNode;
					if (parentNode.Key == "Projectile")
					{
						var projectileInfo = weaponInfo.ProjectileInfos.FirstOrDefault(x => x.Name == parentNode.Value);
						if (projectileInfo.Name != null)
						{
							fieldInfo = projectileInfo.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.Key);
							classFieldInfos = projectileInfo.PropertyInfos;
						}
					}
					else if (parentNode.Key == "Warhead" || parentNode.Key.StartsWith("Warhead@"))
					{
						var warheadInfo = weaponInfo.WarheadInfos.FirstOrDefault(x => x.Name == parentNode.Value);
						if (warheadInfo.Name != null)
						{
							fieldInfo = warheadInfo.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.Key);
							classFieldInfos = warheadInfo.PropertyInfos;
						}
					}

					MapManifest mapManifest = default;
					if (cursorTarget.FileType == FileType.MapRules)
					{
						mapManifest = symbolCache[cursorTarget.ModId].Maps
							.FirstOrDefault(x => x.RulesFiles.Contains(cursorTarget.FileReference));

						if (mapManifest.MapReference != null && symbolCache.Maps.TryGetValue(mapManifest.MapReference, out var mapSymbols))
						{
							paletteNames = paletteNames.Union(mapSymbols.PaletteDefinitions.Select(x => x.First().ToCompletionItem()));
							spriteSequenceImageNames = spriteSequenceImageNames.Union(
								mapSymbols.SpriteSequenceImageDefinitions.Select(x => x.First().ToCompletionItem()));
						}
					}

					if (fieldInfo.OtherAttributes.Any(x => x.Name == "PaletteReference"))
						return paletteNames.Where(x => !string.IsNullOrEmpty(x.Label));

					// Pretend there is such a thing as a "SequenceImageReferenceAttribute" until we add it in OpenRA one day.
					// NOTE: This will improve if/when we add the attribute.
					if (classFieldInfos.Any(x => x.OtherAttributes.Any(y => y.Name == "SequenceReference"
					        && (y.Value.Contains(',') ? y.Value.Substring(0, y.Value.IndexOf(',')) == fieldInfo.Name : y.Value == fieldInfo.Name))))
					{
						return spriteSequenceImageNames;
					}

					if (fieldInfo.OtherAttributes.Any(x => x.Name == "SequenceReference"))
					{
						// Resolve sequence image inheritance so we can show all inherited sequences.
						var imageName = ResolveSpriteSequenceImageNameForWeapons(cursorTarget, fieldInfo, mapManifest);
						var sequences = GetSpriteSequencesForImage(cursorTarget.ModId, imageName, mapManifest);
						return sequences.Select(x => x.ToCompletionItem());
					}

					// Try to check if this is an enum type field.
					var enumInfo = symbolCache[cursorTarget.ModId].CodeSymbols.EnumInfos.FirstOrDefault(x => x.Key == fieldInfo.InternalType);
					if (enumInfo != null)
					{
						return enumInfo.FirstOrDefault().Values.Select(x => new CompletionItem
						{
							Label = x,
							Kind = CompletionItemKind.EnumMember,
							Detail = "Enum type value",
							Documentation = $"{enumInfo.Key}.{x}"
						});
					}

					if (fieldInfo.InternalType == "bool")
					{
						return new[] { trueCompletionItem, falseCompletionItem };
					}

					return Enumerable.Empty<CompletionItem>();
				}

				default:
					return Enumerable.Empty<CompletionItem>();
			}
		}

		protected override IEnumerable<CompletionItem> HandleCursorsValue(CursorTarget cursorTarget)
		{
			// TODO: Return palette information when we have support for palettes.
			return null;
		}

		protected override IEnumerable<CompletionItem> HandleMapFileValue(CursorTarget cursorTarget)
		{
			switch (cursorTarget.TargetNodeIndentation)
			{
				case 0:
					return null;

				case 1:
				{
					if (cursorTarget.TargetNode?.ParentNode?.Key == "Actors")
					{
						// Actor definitions from map rules:
						var mapReference = symbolCache[cursorTarget.ModId].Maps
							.FirstOrDefault(x => x.MapFileReference == cursorTarget.FileReference);

						if (mapReference.MapReference != null && symbolCache.Maps.TryGetValue(mapReference.MapReference, out var mapSymbols))
							return actorNames.Union(mapSymbols.ActorDefinitions.Select(x => x.First().ToCompletionItem()));

						return actorNames;
					}

					return null;
				}

				case 2:
					return null;

				default:
					return null;
			}
		}

		protected override IEnumerable<CompletionItem> HandleSpriteSequenceFileKey(CursorTarget cursorTarget)
		{
			IEnumerable<CompletionItem> HandleSpriteSequenceProperty()
			{
				var presentProperties = cursorTarget.TargetNode.ParentNode.ChildNodes.Select(x => x.Key).ToHashSet();
				var spriteSequenceFormat = symbolCache[modId].ModManifest.SpriteSequenceFormat.Type;

				// NOTE: This is copied from HandleRulesKey()!
				return symbolCache[modId].CodeSymbols.SpriteSequenceInfos[spriteSequenceFormat]
					.SelectMany(x => x.PropertyInfos)
					.DistinctBy(y => y.Name)
					.Where(x => !presentProperties.Contains(x.Name))
					.Select(z => z.ToCompletionItem());
			}

			switch (cursorTarget.TargetNodeIndentation)
			{
				// Get only sprite sequence image definitions. Presumably for reference and for overriding purposes.
				case 0:
					return spriteSequenceImageNames;

				// Get only "Inherits" and "Defaults".
				case 1:
					return new[] { inheritsCompletionItem, defaultsCompletionItem };

				case 2:
					return HandleSpriteSequenceProperty();

				case 4:
				{
					if (cursorTarget.TargetNode.ParentNode.ParentNode.Key == "Combine")
						return HandleSpriteSequenceProperty();

					return Enumerable.Empty<CompletionItem>();
				}

				default:
					return Enumerable.Empty<CompletionItem>();
			}
		}

		protected override IEnumerable<CompletionItem> HandleSpriteSequenceFileValue(CursorTarget cursorTarget)
		{
			IEnumerable<CompletionItem> HandleSpriteSequenceProperty()
			{
				var spriteSequenceFormat = symbolCache[cursorTarget.ModId].ModManifest.SpriteSequenceFormat.Type;
				var spriteSequenceType = symbolCache[cursorTarget.ModId].CodeSymbols.SpriteSequenceInfos[spriteSequenceFormat].First();

				var fieldInfo = spriteSequenceType.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.Key);
				if (fieldInfo.Name != null)
				{
					// Try to check if this is an enum type field.
					var enumInfo = symbolCache[cursorTarget.ModId].CodeSymbols.EnumInfos
						.FirstOrDefault(x => x.Key == fieldInfo.InternalType);
					if (enumInfo != null)
					{
						return enumInfo.FirstOrDefault().Values.Select(x => new CompletionItem
						{
							Label = x,
							Kind = CompletionItemKind.EnumMember,
							Detail = "Enum type value",
							Documentation = $"{enumInfo.Key}.{x}"
						});
					}
				}

				if (fieldInfo.InternalType == "bool")
				{
					return new[] { trueCompletionItem, falseCompletionItem };
				}

				return Enumerable.Empty<CompletionItem>();
			}

			switch (cursorTarget.TargetNodeIndentation)
			{
				case 1:
				{
					if (cursorTarget.TargetNode.Key == "Inherits")
					{
						if (cursorTarget.FileType == FileType.MapSpriteSequences)
						{
							var mapReference = symbolCache[cursorTarget.ModId].Maps
								.FirstOrDefault(x => x.SpriteSequenceFiles.Contains(cursorTarget.FileReference));

							if (mapReference.MapReference != null && symbolCache.Maps.TryGetValue(mapReference.MapReference, out var mapSymbols))
								return spriteSequenceImageNames.Union(mapSymbols.SpriteSequenceImageDefinitions.Select(x => x.First().ToCompletionItem()));
						}

						return spriteSequenceImageNames;
					}

					return Enumerable.Empty<CompletionItem>();
				}

				case 2:
					return HandleSpriteSequenceProperty();

				case 4:
				{
					if (cursorTarget.TargetNode.ParentNode.ParentNode.Key == "Combine")
						return HandleSpriteSequenceProperty();

					return Enumerable.Empty<CompletionItem>();
				}

				default:
					return Enumerable.Empty<CompletionItem>();
			}
		}

		#endregion
	}
}
