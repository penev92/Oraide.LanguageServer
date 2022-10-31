using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LspTypes;
using Oraide.Core;
using Oraide.Core.Entities.Csharp;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;
using Oraide.LanguageServer.Caching;
using Oraide.LanguageServer.Caching.Entities;
using Oraide.LanguageServer.Extensions;
using Range = LspTypes.Range;

namespace Oraide.LanguageServer.LanguageServerProtocolHandlers.TextDocument
{
	public class TextDocumentHoverHandler : BaseRpcMessageHandler
	{
		Range range;
		ModSymbols modSymbols;
		CodeSymbols codeSymbols;

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
						Console.Error.WriteLine("<-- TextDocument-Hover");

					return HandlePositionalRequest(positionParams) as Hover;
				}
				catch (Exception e)
				{
					Console.Error.WriteLine("EXCEPTION!!!");
					Console.Error.WriteLine(e.ToString());
				}

				return null;
			}
		}

		protected override void Initialize(CursorTarget cursorTarget)
		{
			range = cursorTarget.ToRange();
			modSymbols = symbolCache[cursorTarget.ModId].ModSymbols;
			codeSymbols = symbolCache[cursorTarget.ModId].CodeSymbols;
		}

		#region CursorTarget handlers

		protected override Hover HandleRulesKey(CursorTarget cursorTarget)
		{
			switch (cursorTarget.TargetNodeIndentation)
			{
				case 0:
				{
					if (modSymbols.ActorDefinitions.Contains(cursorTarget.TargetString))
					{
						string content;
						if (cursorTarget.TargetString.StartsWith('^'))
							content = $"```csharp\nAbstract Actor \"{cursorTarget.TargetString}\"\n```\n" +
							           $"Abstract actor definitions are meant to be inherited and will not be considered as real actors by the game.";
						else
							content = $"```csharp\nActor \"{cursorTarget.TargetString}\"\n```";

						return HoverFromHoverInfo(content, range);
					}

					if (cursorTarget.FileType == FileType.MapRules)
					{
						var mapReference = symbolCache[cursorTarget.ModId].Maps
							.FirstOrDefault(x => x.RulesFiles.Contains(cursorTarget.FileReference));

						if (mapReference.MapReference != null && symbolCache.Maps.TryGetValue(mapReference.MapReference, out var mapSymbols))
							if (mapSymbols.ActorDefinitions.Contains(cursorTarget.TargetString))
								return HoverFromHoverInfo($"```csharp\nActor \"{cursorTarget.TargetString}\"\n```", range);
					}

					return null;
				}

				case 1:
				{
					if (cursorTarget.TargetString == "Inherits")
						return HoverFromHoverInfo($"Inherits (and possibly overwrites) rules from actor {cursorTarget.TargetNode.Value}", range);

					var traitInfoName = $"{cursorTarget.TargetString}Info";
					if (codeSymbols.TraitInfos.Contains(traitInfoName))
					{
						// Using .First() is not great but we have no way to differentiate between traits of the same name
						// until the server learns the concept of a mod and loaded assemblies.
						var traitInfo = codeSymbols.TraitInfos[traitInfoName].First();
						var content = traitInfo.ToMarkdownInfoString() + "\n\n" + "https://docs.openra.net/en/release/traits/#" + $"{traitInfo.TraitName.ToLower()}";
						return HoverFromHoverInfo(content, range);
					}

					if (cursorTarget.TargetString[0] == '-')
					{
						traitInfoName = traitInfoName.Substring(1);
						if (codeSymbols.TraitInfos.Contains(traitInfoName))
						{
							var modData = symbolCache[cursorTarget.ModId];
							var fileList = modData.ModManifest.RulesFiles;
							var resolvedFileList = fileList.Select(x => OpenRaFolderUtils.ResolveFilePath(x, (modData.ModId, modData.ModFolder)));

							if (TryMergeYamlFiles(resolvedFileList, out _))
								return HoverFromHoverInfo($"Removes trait `{cursorTarget.TargetString.Substring(1)}` from the actor.", range);
						}
					}

					return null;
				}

				case 2:
				{
					var traitInfoName = $"{cursorTarget.TargetNode.ParentNode.Key.Split("@")[0]}Info";
					if (codeSymbols.TraitInfos.Contains(traitInfoName))
					{
						// Using .First() is not great but we have no way to differentiate between traits of the same name
						// until the server learns the concept of a mod and loaded assemblies.
						var traitInfo = codeSymbols.TraitInfos[traitInfoName].First();
						var fieldInfo = traitInfo.TraitPropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetString);
						var content = fieldInfo.ToMarkdownInfoString();
						return HoverFromHoverInfo(content, range);
					}

					return null;
				}

				default:
					return null;
			}
		}

		protected override Hover HandleRulesValue(CursorTarget cursorTarget)
		{
			switch (cursorTarget.TargetNodeIndentation)
			{
				case 0:
					return null;

				case 1:
				{
					if (cursorTarget.TargetNode.Key == "Inherits" || cursorTarget.TargetNode.Key.StartsWith("Inherits@"))
					{
						if (modSymbols.ActorDefinitions.Contains(cursorTarget.TargetString))
							return HoverFromHoverInfo($"```csharp\nActor \"{cursorTarget.TargetString}\"\n```", range);

						if (cursorTarget.FileType == FileType.MapRules)
						{
							var mapReference = symbolCache[cursorTarget.ModId].Maps
								.FirstOrDefault(x => x.RulesFiles.Contains(cursorTarget.FileReference));

							if (mapReference.MapReference != null && symbolCache.Maps.TryGetValue(mapReference.MapReference, out var mapSymbols))
								if (mapSymbols.ActorDefinitions.Contains(cursorTarget.TargetString))
									return HoverFromHoverInfo($"```csharp\nActor \"{cursorTarget.TargetString}\"\n```", range);
						}
					}

					return null;
				}

				case 2:
				{
					var traitInfoName = $"{cursorTarget.TargetNode.ParentNode.Key.Split("@")[0]}Info";
					if (codeSymbols.TraitInfos.Contains(traitInfoName))
					{
						// Using .First() is not great but we have no way to differentiate between traits of the same name
						// until the server learns the concept of a mod and loaded assemblies.
						var traitInfo = codeSymbols.TraitInfos[traitInfoName].First();
						var fieldInfo = traitInfo.TraitPropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.Key);
						if (fieldInfo.Name != null)
						{
							var actorDefinitions = modSymbols.ActorDefinitions.Select(x => x.Key);
							var weaponDefinitions = modSymbols.WeaponDefinitions.Select(x => x.Key);
							var conditionDefinitions = modSymbols.ConditionDefinitions.Select(x => x.Key);
							var cursorDefinitions = modSymbols.CursorDefinitions.Select(x => x.Key);
							var paletteDefinitions = modSymbols.PaletteDefinitions;
							var spriteSequenceImageDefinitions = modSymbols.SpriteSequenceImageDefinitions;

							MapManifest mapManifest = default;
							if (cursorTarget.FileType == FileType.MapRules)
							{
								mapManifest = symbolCache[cursorTarget.ModId].Maps
									.FirstOrDefault(x => x.RulesFiles.Contains(cursorTarget.FileReference));

								if (mapManifest.MapReference != null && symbolCache.Maps.TryGetValue(mapManifest.MapReference, out var mapSymbols))
								{
									actorDefinitions = actorDefinitions.Union(mapSymbols.ActorDefinitions.Select(x => x.Key));
									weaponDefinitions = weaponDefinitions.Union(mapSymbols.WeaponDefinitions.Select(x => x.Key));
									conditionDefinitions = conditionDefinitions.Union(mapSymbols.ConditionDefinitions.Select(x => x.Key));

									// Merge mod symbols with map symbols.
									paletteDefinitions = paletteDefinitions
										.SelectMany(x => x)
										.Union(mapSymbols.PaletteDefinitions.SelectMany(x => x))
										.ToLookup(x => x.Name, y => y);
									spriteSequenceImageDefinitions = spriteSequenceImageDefinitions
										.SelectMany(x => x)
										.Union(mapSymbols.SpriteSequenceImageDefinitions.SelectMany(x => x))
										.ToLookup(x => x.Name, y => y);
								}
							}

							if (fieldInfo.OtherAttributes.Any(x => x.Name == "ActorReference") && actorDefinitions.Contains(cursorTarget.TargetString))
								return HoverFromHoverInfo($"```csharp\nActor \"{cursorTarget.TargetString}\"\n```", range);

							if (fieldInfo.OtherAttributes.Any(x => x.Name == "WeaponReference") && weaponDefinitions.Contains(cursorTarget.TargetString))
								return HoverFromHoverInfo($"```csharp\nWeapon \"{cursorTarget.TargetString}\"\n```", range);

							if (fieldInfo.OtherAttributes.Any(x => x.Name == "GrantedConditionReference") && conditionDefinitions.Contains(cursorTarget.TargetString))
								return HoverFromHoverInfo($"```csharp\nCondition \"{cursorTarget.TargetString}\"\n```", range);

							if (fieldInfo.OtherAttributes.Any(x => x.Name == "ConsumedConditionReference") && conditionDefinitions.Contains(cursorTarget.TargetString))
								return HoverFromHoverInfo($"```csharp\nCondition \"{cursorTarget.TargetString}\"\n```", range);

							if (fieldInfo.OtherAttributes.Any(x => x.Name == "CursorReference") && cursorDefinitions.Contains(cursorTarget.TargetString))
							{
								// Maps can't define cursors, so this is fine using mod symbols only.
								var cursor = modSymbols.CursorDefinitions[cursorTarget.TargetString].First();
								return HoverFromHoverInfo(cursor.ToMarkdownInfoString(), range);
							}

							if (fieldInfo.OtherAttributes.Any(x => x.Name == "PaletteReference") && paletteDefinitions.Contains(cursorTarget.TargetString))
							{
								var palette = paletteDefinitions[cursorTarget.TargetString].First();
								return HoverFromHoverInfo(palette.ToMarkdownInfoString(), range);
							}

							// Pretend there is such a thing as a "SequenceImageReferenceAttribute" until we add it in OpenRA one day.
							// NOTE: This will improve if/when we add the attribute.
							if (traitInfo.TraitPropertyInfos.Any(x => x.OtherAttributes.Any(y => y.Name == "SequenceReference"
								    && (y.Value.Contains(',') ? y.Value.Substring(0, y.Value.IndexOf(',')) == fieldInfo.Name : y.Value == fieldInfo.Name)))
							    && spriteSequenceImageDefinitions.Contains(cursorTarget.TargetString))
							{
								var image = spriteSequenceImageDefinitions[cursorTarget.TargetString].First();
								return HoverFromHoverInfo(image.ToMarkdownInfoString(), range);
							}

							if (fieldInfo.OtherAttributes.Any(x => x.Name == "SequenceReference"))
							{
								var imageName = ResolveSpriteSequenceImageNameForRules(cursorTarget, fieldInfo, mapManifest);
								var image = spriteSequenceImageDefinitions[imageName].First();
								var spriteSequence = image.Sequences.First(x => x.Name == cursorTarget.TargetString);
								return HoverFromHoverInfo(spriteSequence.ToMarkdownInfoString(), range);
							}

							// Try to check if this is an enum type field.
							var enumInfo = symbolCache[cursorTarget.ModId].CodeSymbols.EnumInfos.FirstOrDefault(x => x.Key == fieldInfo.InternalType);
							if (enumInfo != null)
							{
								var content = $"```csharp\n{enumInfo.Key}.{cursorTarget.TargetString}\n```";
								return HoverFromHoverInfo(content, range);
							}

							// Show explanation for world range value.
							if (fieldInfo.InternalType == "WDist")
							{
								var whole = 0;
								var parts = cursorTarget.TargetString.Split('c');
								if ((parts.Length == 1 && int.TryParse(parts[0], out var fraction))
								    || (parts.Length == 2 && int.TryParse(parts[0], out whole) && int.TryParse(parts[1], out fraction)))
								{
									var content = $"Range in world distance units equal to {whole} cells and {fraction} distance units (where 1 cell has 1024 units)";
									return HoverFromHoverInfo(content, range);
								}
							}
						}
					}

					return null;
				}

				default:
					return null;
			}
		}

		protected override Hover HandleWeaponKey(CursorTarget cursorTarget)
		{
			switch (cursorTarget.TargetNodeIndentation)
			{
				case 0:
				{
					if (modSymbols.WeaponDefinitions.Contains(cursorTarget.TargetString))
					{
						string content;
						if (cursorTarget.TargetString.StartsWith('^'))
							content = $"```csharp\nAbstract Weapon \"{cursorTarget.TargetString}\"\n```\n" +
							          $"Abstract weapon definitions are meant to be inherited and can not be used directly.";
						else
							content = $"```csharp\nWeapon \"{cursorTarget.TargetString}\"\n```";

						return HoverFromHoverInfo(content, range);
					}

					if (cursorTarget.FileType == FileType.MapWeapons)
					{
						var mapReference = symbolCache[cursorTarget.ModId].Maps
							.FirstOrDefault(x => x.WeaponsFiles.Contains(cursorTarget.FileReference));

						if (mapReference.MapReference != null && symbolCache.Maps.TryGetValue(mapReference.MapReference, out var mapSymbols))
							if (mapSymbols.WeaponDefinitions.Contains(cursorTarget.TargetString))
								return HoverFromHoverInfo($"```csharp\nWeapon \"{cursorTarget.TargetString}\"\n```", range);
					}

					return null;
				}

				case 1:
				{
					if (cursorTarget.TargetString == "Inherits")
						return HoverFromHoverInfo($"Inherits (and possibly overwrites) rules from weapon {cursorTarget.TargetNode.Value}", range);

					if (cursorTarget.TargetString == "Warhead" || cursorTarget.TargetString.StartsWith("Warhead@"))
						return HoverFromHoverInfo("Warhead used by this weapon.", range);

					// Maybe this is a property of WeaponInfo.
					var prop = codeSymbols.WeaponInfo.WeaponPropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetString);
					if (prop.Name != null)
						return HoverFromHoverInfo(prop.ToMarkdownInfoString(), range);

					if (cursorTarget.TargetString[0] == '-')
					{
						var modData = symbolCache[cursorTarget.ModId];
						var fileList = modData.ModManifest.WeaponsFiles;
						var resolvedFileList = fileList.Select(x => OpenRaFolderUtils.ResolveFilePath(x, (modData.ModId, modData.ModFolder)));

						if (TryMergeYamlFiles(resolvedFileList, out _))
							return HoverFromHoverInfo($"Removes `{cursorTarget.TargetNode.Key.Substring(1)}` from the weapon.", range);
					}

					return null;
				}

				case 2:
				{
					var parentNode = cursorTarget.TargetNode.ParentNode;
					if (parentNode.Key == "Projectile")
					{
						var projectileInfo = codeSymbols.WeaponInfo.ProjectileInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.ParentNode.Value);
						if (projectileInfo.Name != null)
						{
							var prop = projectileInfo.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetString);
							if (prop.Name != null)
								return HoverFromHoverInfo(prop.ToMarkdownInfoString(), range);
						}
					}
					else if (parentNode.Key == "Warhead" || parentNode.Key.StartsWith("Warhead@"))
					{
						var warheadInfo = codeSymbols.WeaponInfo.WarheadInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.ParentNode.Value);
						if (warheadInfo.Name != null)
						{
							var prop = warheadInfo.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetString);
							if (prop.Name != null)
								return HoverFromHoverInfo(prop.ToMarkdownInfoString(), range);
						}
					}

					return null;
				}

				default:
					return null;
			}
		}

		protected override Hover HandleWeaponValue(CursorTarget cursorTarget)
		{
			switch (cursorTarget.TargetNodeIndentation)
			{
				case 0:
					return null;

				case 1:
				{
					var nodeKey = cursorTarget.TargetNode.Key;

					if (nodeKey == "Inherits")
					{
						if (modSymbols.WeaponDefinitions.Any(x => x.Key == cursorTarget.TargetString))
							return HoverFromHoverInfo($"```csharp\nWeapon \"{cursorTarget.TargetString}\"\n```", range);

						if (cursorTarget.FileType == FileType.MapWeapons)
						{
							var mapReference = symbolCache[cursorTarget.ModId].Maps
								.FirstOrDefault(x => x.WeaponsFiles.Contains(cursorTarget.FileReference));

							if (mapReference.MapReference != null && symbolCache.Maps.TryGetValue(mapReference.MapReference, out var mapSymbols))
								if (mapSymbols.WeaponDefinitions.Contains(cursorTarget.TargetString))
									return HoverFromHoverInfo($"```csharp\nWeapon \"{cursorTarget.TargetString}\"\n```", range);
						}
					}
					else if (nodeKey == "Projectile")
					{
						var projectileInfo = codeSymbols.WeaponInfo.ProjectileInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetString);
						if (projectileInfo.Name != null)
						{
							var content = projectileInfo.ToMarkdownInfoString() +
							              "\n\n" + "https://docs.openra.net/en/release/weapons/#" + $"{projectileInfo.Name.ToLower()}";

							return HoverFromHoverInfo(content, range);
						}
					}
					else if (nodeKey == "Warhead" || nodeKey.StartsWith("Warhead@"))
					{
						var warheadInfo = codeSymbols.WeaponInfo.WarheadInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetString);
						if (warheadInfo.Name != null)
						{
							var content = warheadInfo.ToMarkdownInfoString() +
							              "\n\n" + "https://docs.openra.net/en/release/weapons/#" + $"{warheadInfo.InfoName.ToLower()}";

							return HoverFromHoverInfo(content, range);
						}
					}

					var fieldInfo = codeSymbols.WeaponInfo.WeaponPropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.Key);

					// Show explanation for world range value.
					if (fieldInfo.InternalType == "WDist")
					{
						var whole = 0;
						var parts = cursorTarget.TargetString.Split('c');
						if ((parts.Length == 1 && int.TryParse(parts[0], out var fraction))
						    || (parts.Length == 2 && int.TryParse(parts[0], out whole) && int.TryParse(parts[1], out fraction)))
						{
							var content = $"Range in world distance units equal to {whole} cells and {fraction} distance units (where 1 cell has 1024 units)";
							return HoverFromHoverInfo(content, range);
						}
					}

					return null;
				}

				case 2:
				{
					ClassFieldInfo fieldInfo = default;
					var fieldInfos = Array.Empty<ClassFieldInfo>();
					var parentNode = cursorTarget.TargetNode.ParentNode;
					if (parentNode.Key == "Projectile")
					{
						var projectileInfo = codeSymbols.WeaponInfo.ProjectileInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.ParentNode.Value);
						if (projectileInfo.Name != null)
						{
							fieldInfo = projectileInfo.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.Key);
							fieldInfos = projectileInfo.PropertyInfos;
						}
					}
					else if (parentNode.Key == "Warhead" || parentNode.Key.StartsWith("Warhead@"))
					{
						var warheadInfo = codeSymbols.WeaponInfo.WarheadInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.ParentNode.Value);
						if (warheadInfo.Name != null)
						{
							fieldInfo = warheadInfo.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.Key);
							fieldInfos = warheadInfo.PropertyInfos;
						}
					}

					var paletteDefinitions = modSymbols.PaletteDefinitions;
					var spriteSequenceImageDefinitions = modSymbols.SpriteSequenceImageDefinitions;

					MapManifest mapManifest = default;
					if (cursorTarget.FileType == FileType.MapRules)
					{
						mapManifest = symbolCache[cursorTarget.ModId].Maps
							.FirstOrDefault(x => x.RulesFiles.Contains(cursorTarget.FileReference));

						if (mapManifest.MapReference != null && symbolCache.Maps.TryGetValue(mapManifest.MapReference, out var mapSymbols))
						{
							// Merge mod symbols with map symbols.
							paletteDefinitions = paletteDefinitions
								.SelectMany(x => x)
								.Union(mapSymbols.PaletteDefinitions.SelectMany(x => x))
								.ToLookup(x => x.Name, y => y);
							spriteSequenceImageDefinitions = spriteSequenceImageDefinitions
								.SelectMany(x => x)
								.Union(mapSymbols.SpriteSequenceImageDefinitions.SelectMany(x => x))
								.ToLookup(x => x.Name, y => y);
						}
					}

					if (fieldInfo.OtherAttributes.Any(x => x.Name == "PaletteReference") && paletteDefinitions.Contains(cursorTarget.TargetString))
					{
						var palette = paletteDefinitions[cursorTarget.TargetString].First();
						return HoverFromHoverInfo(palette.ToMarkdownInfoString(), range);
					}

					// Pretend there is such a thing as a "SequenceImageReferenceAttribute" until we add it in OpenRA one day.
					// NOTE: This will improve if/when we add the attribute.
					if (fieldInfos.Any(x => x.OtherAttributes.Any(y => y.Name == "SequenceReference"
							&& (y.Value.Contains(',') ? y.Value.Substring(0, y.Value.IndexOf(',')) == fieldInfo.Name : y.Value == fieldInfo.Name)))
					    && spriteSequenceImageDefinitions.Contains(cursorTarget.TargetString))
					{
						var image = spriteSequenceImageDefinitions[cursorTarget.TargetString].First();
						return HoverFromHoverInfo(image.ToMarkdownInfoString(), range);
					}

					if (fieldInfo.OtherAttributes.Any(x => x.Name == "SequenceReference"))
					{
						var imageName = ResolveSpriteSequenceImageNameForWeapons(cursorTarget, fieldInfo, mapManifest);
						var image = spriteSequenceImageDefinitions[imageName].First();
						var spriteSequence = image.Sequences.First(x => x.Name == cursorTarget.TargetString);
						return HoverFromHoverInfo(spriteSequence.ToMarkdownInfoString(), range);
					}

					// Try to check if this is an enum type field.
					var enumInfo = symbolCache[cursorTarget.ModId].CodeSymbols.EnumInfos.FirstOrDefault(x => x.Key == fieldInfo.InternalType);
					if (enumInfo != null)
					{
						var content = $"```csharp\n{enumInfo.Key}.{cursorTarget.TargetString}\n```";
						return HoverFromHoverInfo(content, range);
					}

					// Show explanation for world range value.
					if (fieldInfo.InternalType == "WDist")
					{
						var whole = 0;
						var parts = cursorTarget.TargetString.Split('c');
						if ((parts.Length == 1 && int.TryParse(parts[0], out var fraction))
						    || (parts.Length == 2 && int.TryParse(parts[0], out whole) && int.TryParse(parts[1], out fraction)))
						{
							var content = $"Range in world distance units equal to {whole} cells and {fraction} distance units (where 1 cell has 1024 units)";
							return HoverFromHoverInfo(content, range);
						}
					}

					return null;
				}

				default:
					return null;
			}
		}

		protected override Hover HandleCursorsValue(CursorTarget cursorTarget)
		{
			// TODO: Return palette information when we have support for palettes.
			return null;
		}

		protected override Hover HandleMapFileKey(CursorTarget cursorTarget)
		{
			switch (cursorTarget.TargetNodeIndentation)
			{
				case 1:
				{
					if (cursorTarget.TargetString == "PlayerReference" && cursorTarget.TargetNode?.ParentNode?.Key == "Players")
					{
						// TODO: This could only be useful if documentation for PlayerReference is added.
					}

					if (cursorTarget.TargetNode?.ParentNode?.Key == "Actors")
						return HoverFromHoverInfo("**Actor name**\n\nThis name will be used by potential map scripts and will also show up in the map editor.", range);

					return null;
				}

				case 2:
				{
					if (cursorTarget.TargetNode?.ParentNode?.Key != null && cursorTarget.TargetNode.ParentNode.Key.StartsWith("PlayerReference"))
					{
						// TODO: This could only be useful if documentation for PlayerReference is added.
					}

					if (cursorTarget.TargetNode?.ParentNode?.ParentNode?.Key == "Actors")
					{
						// TODO: Add support for ActorInits one day.
					}

					return null;
				}

				default:
					return null;
			}
		}

		protected override Hover HandleMapFileValue(CursorTarget cursorTarget)
		{
			switch (cursorTarget.TargetNodeIndentation)
			{
				case 0:
				{
					if (cursorTarget.TargetNode.Key is "Rules" or "Sequences" or "ModelSequences" or "Weapons" or "Voices" or "Music" or "Notifications")
					{
						if (cursorTarget.TargetString.Contains('|'))
						{
							var resolvedFile = OpenRaFolderUtils.ResolveFilePath(cursorTarget.TargetString, (cursorTarget.ModId, symbolCache[cursorTarget.ModId].ModFolder));
							if (File.Exists(resolvedFile))
								return HoverFromHoverInfo($"```csharp\nFile \"{cursorTarget.TargetString}\"\n```", range);
						}
						else
						{
							var targetPath = cursorTarget.TargetStart.FileUri.AbsolutePath.Replace("file:///", string.Empty).Replace("%3A", ":");
							var mapFolder = Path.GetDirectoryName(targetPath);
							var mapName = Path.GetFileName(mapFolder);
							var filePath = Path.Combine(mapFolder, cursorTarget.TargetString);
							if (File.Exists(filePath))
								return HoverFromHoverInfo($"```csharp\nFile \"{mapName}/{cursorTarget.TargetString}\"\n```", range);
						}
					}

					return null; // TODO:
				}

				case 1:
				{
					if (cursorTarget.TargetNode?.ParentNode?.Key == "Actors")
					{
						// Actor definitions from the mod rules:
						if (modSymbols.ActorDefinitions.Contains(cursorTarget.TargetString))
							return HoverFromHoverInfo($"```csharp\nActor \"{cursorTarget.TargetString}\"\n```", range);

						// Actor definitions from map rules:
						var mapReference = symbolCache[cursorTarget.ModId].Maps
							.FirstOrDefault(x => x.MapFileReference == cursorTarget.FileReference);

						if (mapReference.MapReference != null && symbolCache.Maps.TryGetValue(mapReference.MapReference, out var mapSymbols))
							if (mapSymbols.ActorDefinitions.Contains(cursorTarget.TargetString))
								return HoverFromHoverInfo($"```csharp\nActor \"{cursorTarget.TargetString}\"\n```", range);
					}

					return null;
				}

				case 2:
				{
					if (cursorTarget.TargetNode?.ParentNode?.ParentNode?.Key == "Players")
					{
						// TODO: Add support for factions and for players.
					}

					if (cursorTarget.TargetNode?.ParentNode?.ParentNode?.Key == "Actors")
					{
						// TODO: Add support for ActorInits one day.
					}

					return null;
				}

				default:
					return null;
			}
		}

		protected override Hover HandleSpriteSequenceFileKey(CursorTarget cursorTarget)
		{
			Hover HandleSpriteSequenceName()
			{
				var content = $"```csharp\nSequence \"{cursorTarget.TargetString}\"\n```";
				return HoverFromHoverInfo(content, range);
			}

			Hover HandleSpriteSequenceProperty()
			{
				var spriteSequenceFormat = symbolCache[cursorTarget.ModId].ModManifest.SpriteSequenceFormat.Type;
				var fieldInfo = codeSymbols.SpriteSequenceInfos[spriteSequenceFormat].FirstOrDefault().PropertyInfos
					.FirstOrDefault(x => x.Name == cursorTarget.TargetString);

				var content = fieldInfo.ToMarkdownInfoString();
				return HoverFromHoverInfo(content, range);
			}

			switch (cursorTarget.TargetNodeIndentation)
			{
				case 0:
				{
					var content = $"```csharp\nImage \"{cursorTarget.TargetString}\"\n```";
					return HoverFromHoverInfo(content, range);
				}

				case 1:
				{
					if (cursorTarget.TargetString == "Inherits")
						return HoverFromHoverInfo($"Inherits (and possibly overwrites) sequences from image {cursorTarget.TargetNode.Value}", range);

					if (cursorTarget.TargetString == "Defaults")
						return HoverFromHoverInfo("Sets default values for all sequences of this image.", range);

					return HandleSpriteSequenceName();
				}

				case 2:
					return HandleSpriteSequenceProperty();

				case 3:
				{
					if (cursorTarget.TargetNode.ParentNode.Key == "Combine")
						return HandleSpriteSequenceName();

					return null;
				}

				case 4:
				{
					if (cursorTarget.TargetNode.ParentNode.ParentNode.Key == "Combine")
						return HandleSpriteSequenceProperty();

					return null;
				}
			}

			return null;
		}

		protected override Hover HandleSpriteSequenceFileValue(CursorTarget cursorTarget)
		{
			Hover HandleSpriteSequenceFileName()
			{
				var content = $"```csharp\nFile \"{cursorTarget.TargetString}\"\n```";
				return HoverFromHoverInfo(content, range);
			}

			Hover HandleSpriteSequenceProperty()
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
						var content = $"```csharp\n{enumInfo.Key}.{cursorTarget.TargetString}\n```";
						return HoverFromHoverInfo(content, range);
					}
				}

				return null;
			}

			switch (cursorTarget.TargetNodeIndentation)
			{
				case 1:
				{
					if (cursorTarget.TargetNode.Key == "Inherits")
					{
						if (modSymbols.SpriteSequenceImageDefinitions.Contains(cursorTarget.TargetString))
							return HoverFromHoverInfo($"```csharp\nImage \"{cursorTarget.TargetString}\"\n```", range);

						if (cursorTarget.FileType == FileType.MapSpriteSequences)
						{
							var mapReference = symbolCache[cursorTarget.ModId].Maps
								.FirstOrDefault(x => x.SpriteSequenceFiles.Contains(cursorTarget.FileReference));

							if (mapReference.MapReference != null && symbolCache.Maps.TryGetValue(mapReference.MapReference, out var mapSymbols))
								if (mapSymbols.SpriteSequenceImageDefinitions.Contains(cursorTarget.TargetString))
									return HoverFromHoverInfo($"```csharp\nImage \"{cursorTarget.TargetString}\"\n```", range);
						}
					}
					else
					{
						return HandleSpriteSequenceFileName();
					}

					return null;
				}

				case 2:
					return HandleSpriteSequenceProperty();

				case 3:
				{
					if (cursorTarget.TargetNode.ParentNode.Key == "Combine")
						return HandleSpriteSequenceFileName();

					return null;
				}

				case 4:
				{
					if (cursorTarget.TargetNode.ParentNode.ParentNode.Key == "Combine")
						return HandleSpriteSequenceProperty();

					return null;
				}

				default:
					return null;
			}
		}

		#endregion

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
