using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LspTypes;
using Oraide.Core;
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
						return HoverFromHoverInfo($"```csharp\nActor \"{cursorTarget.TargetString}\"\n```", range);

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
						var content = traitInfo.ToMarkdownInfoString() + "\n\n" + "https://docs.openra.net/en/latest/release/traits/#" + $"{traitInfo.TraitName.ToLower()}";
						return HoverFromHoverInfo(content, range);
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
							if (cursorTarget.FileType == FileType.MapRules)
							{
								var mapReference = symbolCache[cursorTarget.ModId].Maps
									.FirstOrDefault(x => x.RulesFiles.Contains(cursorTarget.FileReference));

								if (mapReference.MapReference != null && symbolCache.Maps.TryGetValue(mapReference.MapReference, out var mapSymbols))
								{
									actorDefinitions = actorDefinitions.Union(mapSymbols.ActorDefinitions.Select(x => x.Key));
									weaponDefinitions = weaponDefinitions.Union(mapSymbols.WeaponDefinitions.Select(x => x.Key));
									conditionDefinitions = conditionDefinitions.Union(mapSymbols.ConditionDefinitions.Select(x => x.Key));
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
								var cursor = modSymbols.CursorDefinitions[cursorTarget.TargetString].First();
								return HoverFromHoverInfo(cursor.ToMarkdownInfoString(), range);
							}
						}

						// Show explanation for world range value.
						if (Regex.IsMatch(cursorTarget.TargetString, "[0-9]+c[0-9]+", RegexOptions.Compiled))
						{
							var parts = cursorTarget.TargetString.Split('c');
							var content = $"Range in world distance units equal to {parts[0]} cells and {parts[1]} distance units (where 1 cell has 1024 units)";
							return HoverFromHoverInfo(content, range);
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
						return HoverFromHoverInfo($"```csharp\nWeapon \"{cursorTarget.TargetString}\"\n```", range);

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
							              "\n\n" + "https://docs.openra.net/en/latest/release/weapons/#" + $"{projectileInfo.Name.ToLower()}";

							return HoverFromHoverInfo(content, range);
						}
					}
					else if (nodeKey == "Warhead" || nodeKey.StartsWith("Warhead@"))
					{
						var warheadInfo = codeSymbols.WeaponInfo.WarheadInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetString);
						if (warheadInfo.Name != null)
						{
							var content = warheadInfo.ToMarkdownInfoString() +
							              "\n\n" + "https://docs.openra.net/en/latest/release/weapons/#" + $"{warheadInfo.Name.ToLower()}";

							return HoverFromHoverInfo(content, range);
						}
					}

					// Show explanation for world range value.
					if (Regex.IsMatch(cursorTarget.TargetString, "[0-9]+c[0-9]+", RegexOptions.Compiled))
					{
						var parts = cursorTarget.TargetString.Split('c');
						var content = $"Range in world distance units equal to {parts[0]} cells and {parts[1]} distance units (where 1 cell has 1024 units)";
						return HoverFromHoverInfo(content, range);
					}

					return null;
				}

				case 2:
				{
					// Show explanation for world range value.
					if (Regex.IsMatch(cursorTarget.TargetString, "[0-9]+c[0-9]+", RegexOptions.Compiled))
					{
						var parts = cursorTarget.TargetString.Split('c');
						var content = $"Range in world distance units equal to {parts[0]} cells and {parts[1]} distance units (where 1 cell has 1024 units)";
						return HoverFromHoverInfo(content, range);
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
							var targetPath = cursorTarget.TargetStart.FilePath.Replace("file:///", string.Empty).Replace("%3A", ":");
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
