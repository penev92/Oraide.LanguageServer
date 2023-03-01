﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LspTypes;
using Oraide.Core;
using Oraide.Core.Entities.Csharp;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;
using Oraide.LanguageServer.Caching;
using Oraide.LanguageServer.Caching.Entities;
using Oraide.LanguageServer.Extensions;

namespace Oraide.LanguageServer.LanguageServerProtocolHandlers.TextDocument
{
	public class TextDocumentDefinitionHandler : BaseRpcMessageHandler
	{
		ModSymbols modSymbols;
		WeaponInfo weaponInfo;
		CodeSymbols codeSymbols;

		public TextDocumentDefinitionHandler(SymbolCache symbolCache, OpenFileCache openFileCache)
			: base(symbolCache, openFileCache) { }

		[OraideCustomJsonRpcMethodTag(Methods.TextDocumentDefinitionName)]
		public IEnumerable<Location> Definition(TextDocumentPositionParams positionParams)
		{
			lock (LockObject)
			{
				try
				{
					if (trace)
						Console.Error.WriteLine("<-- TextDocument-Definition");

					return HandlePositionalRequest(positionParams) as IEnumerable<Location>;
				}
				catch (Exception e)
				{
					Console.Error.WriteLine("EXCEPTION!!!");
					Console.Error.WriteLine(e.ToString());
				}

				return Enumerable.Empty<Location>();
			}
		}

		protected override void Initialize(CursorTarget cursorTarget)
		{
			modSymbols = symbolCache[cursorTarget.ModId].ModSymbols;
			codeSymbols = symbolCache[cursorTarget.ModId].CodeSymbols;
			weaponInfo = symbolCache[cursorTarget.ModId].CodeSymbols.WeaponInfo;
		}

		#region CursorTarget handlers

		protected override IEnumerable<Location> HandleRulesKey(CursorTarget cursorTarget)
		{
			switch (cursorTarget.TargetNodeIndentation)
			{
				case 0:
					return Enumerable.Empty<Location>();

				case 1:
				{
					var traitName = cursorTarget.TargetNode.Key.Split('@')[0];
					var traitInfoName = $"{traitName}Info";

					// Using .First() is not great but we have no way to differentiate between traits of the same name
					// until the server learns the concept of a mod and loaded assemblies.
					var traitInfo = codeSymbols.TraitInfos[traitInfoName].FirstOrDefault();
					if (traitInfo.Name != null)
						return new[] { traitInfo.Location.ToLspLocation(cursorTarget.TargetString.Length) };

					return Enumerable.Empty<Location>();
				}

				case 2:
				{
					var traitName = cursorTarget.TargetNode.ParentNode.Key.Split('@')[0];
					var traitInfoName = $"{traitName}Info";

					// Using .First() is not great but we have no way to differentiate between traits of the same name
					// until the server learns the concept of a mod and loaded assemblies.
					var traitInfo = codeSymbols.TraitInfos[traitInfoName].FirstOrDefault();
					if (traitInfo.Name != null)
					{
						var fieldInfo = traitInfo.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.Key);
						if (fieldInfo.Name != null)
							return new[] { fieldInfo.Location.ToLspLocation(cursorTarget.TargetString.Length) };
					}

					return Enumerable.Empty<Location>();
				}

				default:
					return Enumerable.Empty<Location>();
			}
		}

		protected override IEnumerable<Location> HandleRulesValue(CursorTarget cursorTarget)
		{
			switch (cursorTarget.TargetNodeIndentation)
			{
				case 0:
					return Enumerable.Empty<Location>();

				case 1:
				{
					// Get actor definitions (for inheriting).
					if (cursorTarget.TargetNode.Key == "Inherits" || cursorTarget.TargetNode.Key.StartsWith("Inherits@"))
					{
						if (modSymbols.ActorDefinitions.Contains(cursorTarget.TargetString))
							return modSymbols.ActorDefinitions[cursorTarget.TargetString].Select(x => x.Location.ToLspLocation(x.Name.Length));

						if (cursorTarget.FileType == FileType.MapRules)
						{
							var mapReference = symbolCache[cursorTarget.ModId].Maps
								.FirstOrDefault(x => x.RulesFiles.Contains(cursorTarget.FileReference));

							if (mapReference.MapReference != null && symbolCache.Maps.TryGetValue(mapReference.MapReference, out var mapSymbols))
								if (mapSymbols.ActorDefinitions.Contains(cursorTarget.TargetString))
									return mapSymbols.ActorDefinitions[cursorTarget.TargetString].Select(x => x.Location.ToLspLocation(x.Name.Length));
						}
					}

					return Enumerable.Empty<Location>();
				}

				case 2:
				{
					var traitName = cursorTarget.TargetNode.ParentNode.Key.Split('@')[0];
					var traitInfoName = $"{traitName}Info";

					// Using .First() is not great but we have no way to differentiate between traits of the same name
					// until the server learns the concept of a mod and loaded assemblies.
					var traitInfo = codeSymbols.TraitInfos[traitInfoName].FirstOrDefault();
					if (traitInfo.Name != null)
					{
						var fieldInfo = traitInfo.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.Key);
						if (fieldInfo.Name != null)
						{
							var actorDefinitions = modSymbols.ActorDefinitions[cursorTarget.TargetString];
							var weaponDefinitions = modSymbols.WeaponDefinitions[cursorTarget.TargetString];
							var conditionDefinitions = modSymbols.ConditionDefinitions[cursorTarget.TargetString];
							var cursorDefinitions = modSymbols.CursorDefinitions[cursorTarget.TargetString];
							var paletteDefinitions = modSymbols.PaletteDefinitions[cursorTarget.TargetString];
							var spriteSequenceImageDefinitions = modSymbols.SpriteSequenceImageDefinitions;

							MapManifest mapManifest = default;
							if (cursorTarget.FileType == FileType.MapRules)
							{
								mapManifest = symbolCache[cursorTarget.ModId].Maps
									.FirstOrDefault(x => x.RulesFiles.Contains(cursorTarget.FileReference));

								if (mapManifest.MapReference != null && symbolCache.Maps.TryGetValue(mapManifest.MapReference, out var mapSymbols))
								{
									actorDefinitions = actorDefinitions.Union(mapSymbols.ActorDefinitions[cursorTarget.TargetString]);
									weaponDefinitions = weaponDefinitions.Union(mapSymbols.WeaponDefinitions[cursorTarget.TargetString]);
									conditionDefinitions = conditionDefinitions.Union(mapSymbols.ConditionDefinitions[cursorTarget.TargetString]);
									paletteDefinitions = paletteDefinitions.Union(mapSymbols.PaletteDefinitions[cursorTarget.TargetString]);

									spriteSequenceImageDefinitions = spriteSequenceImageDefinitions
										.SelectMany(x => x)
										.Union(mapSymbols.SpriteSequenceImageDefinitions.SelectMany(x => x))
										.ToLookup(x => x.Name, y => y);
								}
							}

							if (fieldInfo.OtherAttributes.Any(x => x.Name == "ActorReference"))
								return actorDefinitions.Select(x => x.Location.ToLspLocation(x.Name.Length));

							if (fieldInfo.OtherAttributes.Any(x => x.Name == "WeaponReference"))
								return weaponDefinitions.Select(x => x.Location.ToLspLocation(x.Name.Length));

							if (fieldInfo.OtherAttributes.Any(x => x.Name == "GrantedConditionReference"))
								return conditionDefinitions.Select(x => x.Location.ToLspLocation(x.Name.Length));

							if (fieldInfo.OtherAttributes.Any(x => x.Name == "ConsumedConditionReference"))
								return conditionDefinitions.Select(x => x.Location.ToLspLocation(x.Name.Length));

							if (fieldInfo.OtherAttributes.Any(x => x.Name == "CursorReference"))
								return cursorDefinitions.Select(x => x.Location.ToLspLocation(x.Name.Length));

							if (fieldInfo.OtherAttributes.Any(x => x.Name == "PaletteReference"))
								return paletteDefinitions.Select(x => x.Location.ToLspLocation(x.Type.Length));

							// Pretend there is such a thing as a "SequenceImageReferenceAttribute" until we add it in OpenRA one day.
							// NOTE: This will improve if/when we add the attribute.
							if (traitInfo.PropertyInfos.Any(x => x.OtherAttributes.Any(y => y.Name == "SequenceReference"
									&& !string.IsNullOrWhiteSpace(y.Value)
								    && (y.Value.Contains(',') ? y.Value.Substring(0, y.Value.IndexOf(',')) == fieldInfo.Name : y.Value == fieldInfo.Name))))
							{
								return spriteSequenceImageDefinitions[cursorTarget.TargetString].Select(x => x.Location.ToLspLocation(x.Name.Length));
							}

							if (fieldInfo.OtherAttributes.Any(x => x.Name == "SequenceReference"))
							{
								var imageName = ResolveSpriteSequenceImageNameForRules(cursorTarget, fieldInfo, mapManifest);
								return spriteSequenceImageDefinitions[imageName].SelectMany(x => x.Sequences)
									.Where(x => x.Name == cursorTarget.TargetString)
									.Select(x => x.Location.ToLspLocation(x.Name.Length));
							}

							// Try to check if this is an enum type field.
							var enumInfo = symbolCache[cursorTarget.ModId].CodeSymbols.EnumInfos
								.FirstOrDefault(x => x.Key == fieldInfo.InternalType);
							if (enumInfo != null)
							{
								return new[] { enumInfo.First().Location.ToLspLocation(enumInfo.Key.Length) };
							}
						}
					}

					return Enumerable.Empty<Location>();
				}

				default:
					return Enumerable.Empty<Location>();
			}
		}

		protected override IEnumerable<Location> HandleWeaponKey(CursorTarget cursorTarget)
		{
			switch (cursorTarget.TargetNodeIndentation)
			{
				case 0:
				{
					var weaponDefinitions = modSymbols.WeaponDefinitions.FirstOrDefault(x => x.Key == cursorTarget.TargetString);
					if (weaponDefinitions != null)
						return weaponDefinitions.Select(x => x.Location.ToLspLocation(weaponDefinitions.Key.Length));

					return Enumerable.Empty<Location>();
				}

				case 1:
				{
					var targetString = cursorTarget.TargetString;
					if (cursorTarget.TargetString == "Warhead")
						targetString = "Warheads"; // Hacks!

					var fieldInfo = weaponInfo.WeaponPropertyInfos.FirstOrDefault(x => x.Name == targetString);
					if (fieldInfo.Name != null)
						return new[] { fieldInfo.Location.ToLspLocation(fieldInfo.Name.Length) };

					return Enumerable.Empty<Location>();
				}

				case 2:
				{
					var parentNodeKey = cursorTarget.TargetNode.ParentNode.Key;
					if (parentNodeKey == "Projectile")
					{
						var projectile = weaponInfo.ProjectileInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.ParentNode.Value);
						if (projectile.Name != null)
						{
							var fieldInfo = projectile.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetString);
							if (fieldInfo.Name != null)
								return new[] { fieldInfo.Location.ToLspLocation(fieldInfo.Name.Length) };
						}
					}
					else if (parentNodeKey == "Warhead" || parentNodeKey.StartsWith("Warhead@"))
					{
						var warhead = weaponInfo.WarheadInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.ParentNode.Value);
						if (warhead.Name != null)
						{
							var fieldInfo = warhead.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetString);
							if (fieldInfo.Name != null)
								return new[] { fieldInfo.Location.ToLspLocation(fieldInfo.Name.Length) };
						}
					}

					return Enumerable.Empty<Location>();
				}

				default:
					return Enumerable.Empty<Location>();
			}
		}

		protected override IEnumerable<Location> HandleWeaponValue(CursorTarget cursorTarget)
		{
			switch (cursorTarget.TargetNodeIndentation)
			{
				case 0:
					return Enumerable.Empty<Location>();

				case 1:
				{
					var targetNodeKey = cursorTarget.TargetNode.Key;
					if (targetNodeKey == "Inherits")
					{
						var weaponDefinitions = modSymbols.WeaponDefinitions.FirstOrDefault(x => x.Key == cursorTarget.TargetString);
						if (weaponDefinitions != null)
							return weaponDefinitions.Select(x => x.Location.ToLspLocation(weaponDefinitions.Key.Length));

						if (cursorTarget.FileType == FileType.MapWeapons)
						{
							var mapReference = symbolCache[cursorTarget.ModId].Maps
								.FirstOrDefault(x => x.WeaponsFiles.Contains(cursorTarget.FileReference));

							if (mapReference.MapReference != null && symbolCache.Maps.TryGetValue(mapReference.MapReference, out var mapSymbols))
								if (mapSymbols.WeaponDefinitions.Contains(cursorTarget.TargetString))
									return mapSymbols.WeaponDefinitions[cursorTarget.TargetString].Select(x => x.Location.ToLspLocation(x.Name.Length));
						}
					}
					else if (targetNodeKey == "Projectile")
					{
						var projectile = weaponInfo.ProjectileInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetString);
						if (projectile.Name != null)
							return new[] { projectile.Location.ToLspLocation(projectile.Name.Length) };
					}
					else if (targetNodeKey == "Warhead" || targetNodeKey.StartsWith("Warhead@"))
					{
						var warhead = weaponInfo.WarheadInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetString);
						if (warhead.Name != null)
							return new[] { warhead.Location.ToLspLocation(warhead.Name.Length) };
					}

					return Enumerable.Empty<Location>();
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

					var paletteDefinitions = modSymbols.PaletteDefinitions[cursorTarget.TargetString];
					var spriteSequenceImageDefinitions = symbolCache[cursorTarget.ModId].ModSymbols.SpriteSequenceImageDefinitions;

					MapManifest mapManifest = default;
					if (cursorTarget.FileType == FileType.MapRules)
					{
						mapManifest = symbolCache[cursorTarget.ModId].Maps
							.FirstOrDefault(x => x.RulesFiles.Contains(cursorTarget.FileReference));

						if (mapManifest.MapReference != null && symbolCache.Maps.TryGetValue(mapManifest.MapReference, out var mapSymbols))
						{
							// Merge mod symbols with map symbols.
							paletteDefinitions = paletteDefinitions.Union(mapSymbols.PaletteDefinitions[cursorTarget.TargetString]);
							spriteSequenceImageDefinitions = spriteSequenceImageDefinitions
								.SelectMany(x => x)
								.Union(mapSymbols.SpriteSequenceImageDefinitions.SelectMany(x => x))
								.ToLookup(x => x.Name, y => y);
						}
					}

					if (fieldInfo.OtherAttributes.Any(x => x.Name == "PaletteReference"))
						return paletteDefinitions.Select(x => x.Location.ToLspLocation(x.Type.Length));

					// Pretend there is such a thing as a "SequenceImageReferenceAttribute" until we add it in OpenRA one day.
					// NOTE: This will improve if/when we add the attribute.
					if (fieldInfos.Any(x => x.OtherAttributes.Any(y => y.Name == "SequenceReference"
					        && (y.Value.Contains(',') ? y.Value.Substring(0, y.Value.IndexOf(',')) == fieldInfo.Name : y.Value == fieldInfo.Name))))
					{
						return spriteSequenceImageDefinitions[cursorTarget.TargetString].Select(x => x.Location.ToLspLocation(x.Name.Length));
					}

					if (fieldInfo.OtherAttributes.Any(x => x.Name == "SequenceReference"))
					{
						var imageName = ResolveSpriteSequenceImageNameForWeapons(cursorTarget, fieldInfo, mapManifest);
						return spriteSequenceImageDefinitions[imageName].SelectMany(x => x.Sequences)
							.Where(x => x.Name == cursorTarget.TargetString)
							.Select(x => x.Location.ToLspLocation(x.Name.Length));
					}

					// Try to check if this is an enum type field.
					var enumInfo = symbolCache[cursorTarget.ModId].CodeSymbols.EnumInfos
						.FirstOrDefault(x => x.Key == fieldInfo.InternalType);
					if (enumInfo != null)
					{
						return new[] { enumInfo.First().Location.ToLspLocation(enumInfo.Key.Length) };
					}

					return Enumerable.Empty<Location>();
				}

				default:
					return Enumerable.Empty<Location>();
			}
		}

		protected override IEnumerable<Location> HandleCursorsValue(CursorTarget cursorTarget)
		{
			// TODO: Return palette information when we have support for palettes.
			return null;
		}

		protected override IEnumerable<Location> HandleMapFileValue(CursorTarget cursorTarget)
		{
			switch (cursorTarget.TargetNodeIndentation)
			{
				case 0:
				{
					string filePath = null;
					if (cursorTarget.TargetNode.Key is "Rules" or "Sequences" or "ModelSequences" or "Weapons" or "Voices" or "Music" or "Notifications")
					{
						if (cursorTarget.TargetString.Contains('|'))
						{
							filePath = OpenRaFolderUtils.ResolveFilePath(cursorTarget.TargetString, (cursorTarget.ModId, symbolCache[cursorTarget.ModId].ModFolder));
						}
						else
						{
							var targetPath = cursorTarget.TargetStart.FileUri.AbsolutePath;
							var mapFolder = Path.GetDirectoryName(targetPath);
							filePath = Path.Combine(mapFolder, cursorTarget.TargetString);
						}
					}

					if (filePath != null && File.Exists(filePath))
					{
						return new[]
						{
							new Location
							{
								Uri = new Uri(filePath).ToString(),
								Range = new LspTypes.Range
								{
									Start = new Position(0, 0),
									End = new Position(0, 0)
								}
							}
						};
					}

					return null;
				}

				case 1:
				{
					if (cursorTarget.TargetNode?.ParentNode?.Key == "Actors")
					{
						// Actor definitions from the mod rules:
						if (modSymbols.ActorDefinitions.Contains(cursorTarget.TargetString))
							return modSymbols.ActorDefinitions[cursorTarget.TargetString].Select(x => x.Location.ToLspLocation(x.Name.Length));

						// Actor definitions from map rules:
						var mapReference = symbolCache[cursorTarget.ModId].Maps
							.FirstOrDefault(x => x.MapFileReference == cursorTarget.FileReference);

						if (mapReference.MapReference != null && symbolCache.Maps.TryGetValue(mapReference.MapReference, out var mapSymbols))
							if (mapSymbols.ActorDefinitions.Contains(cursorTarget.TargetString))
								return mapSymbols.ActorDefinitions[cursorTarget.TargetString].Select(x => x.Location.ToLspLocation(x.Name.Length));
					}

					return Enumerable.Empty<Location>();
				}

				case 2:
					return Enumerable.Empty<Location>();

				default:
					return Enumerable.Empty<Location>();
			}
		}

		// NOTE: Rules and Weapons don't handle these the same ;(
		protected override IEnumerable<Location> HandleSpriteSequenceFileKey(CursorTarget cursorTarget)
		{
			IEnumerable<Location> HandleSpriteSequenceProperty()
			{
				var spriteSequenceFormat = symbolCache[cursorTarget.ModId].ModManifest.SpriteSequenceFormat.Type;
				var spriteSequenceType = symbolCache[cursorTarget.ModId].CodeSymbols.SpriteSequenceInfos[spriteSequenceFormat].First();

				var fieldInfo = spriteSequenceType.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.Key);
				if (fieldInfo.Name != null)
					return new[] { fieldInfo.Location.ToLspLocation(cursorTarget.TargetString.Length) };

				return Enumerable.Empty<Location>();
			}

			switch (cursorTarget.TargetNodeIndentation)
			{
				// NOTE: Copied from HandleWeaponsFileKey.
				case 0:
				{
					var imageDefinitions = modSymbols.SpriteSequenceImageDefinitions.FirstOrDefault(x => x.Key == cursorTarget.TargetString);
					if (imageDefinitions != null)
						return imageDefinitions.Select(x => x.Location.ToLspLocation(imageDefinitions.Key.Length));

					return Enumerable.Empty<Location>();
				}

				case 2:
					return HandleSpriteSequenceProperty();

				case 4:
				{
					if (cursorTarget.TargetNode.ParentNode.ParentNode.Key == "Combine")
						return HandleSpriteSequenceProperty();

					return Enumerable.Empty<Location>();
				}

				default:
					return Enumerable.Empty<Location>();
			}
		}

		protected override IEnumerable<Location> HandleSpriteSequenceFileValue(CursorTarget cursorTarget)
		{
			IEnumerable<Location> HandleSpriteSequenceProperty()
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
						return new[] { enumInfo.First().Location.ToLspLocation(enumInfo.Key.Length) };
					}
				}

				return Enumerable.Empty<Location>();
			}

			switch (cursorTarget.TargetNodeIndentation)
			{
				// NOTE: Copied from HandleWeaponsFileKey.
				case 1:
				{
					if (cursorTarget.TargetNode.Key == "Inherits")
					{
						if (modSymbols.SpriteSequenceImageDefinitions.Contains(cursorTarget.TargetString))
							return modSymbols.SpriteSequenceImageDefinitions[cursorTarget.TargetString].Select(x => x.Location.ToLspLocation(x.Name.Length));

						if (cursorTarget.FileType == FileType.MapRules)
						{
							var mapReference = symbolCache[cursorTarget.ModId].Maps
								.FirstOrDefault(x => x.SpriteSequenceFiles.Contains(cursorTarget.FileReference));

							if (mapReference.MapReference != null && symbolCache.Maps.TryGetValue(mapReference.MapReference, out var mapSymbols))
								if (mapSymbols.SpriteSequenceImageDefinitions.Contains(cursorTarget.TargetString))
									return mapSymbols.SpriteSequenceImageDefinitions[cursorTarget.TargetString].Select(x => x.Location.ToLspLocation(x.Name.Length));
						}
					}

					return Enumerable.Empty<Location>();
				}

				case 2:
					return HandleSpriteSequenceProperty();

				case 4:
				{
					if (cursorTarget.TargetNode.ParentNode.ParentNode.Key == "Combine")
						return HandleSpriteSequenceProperty();

					return Enumerable.Empty<Location>();
				}

				default:
					return Enumerable.Empty<Location>();
			}
		}

		#endregion
	}
}
