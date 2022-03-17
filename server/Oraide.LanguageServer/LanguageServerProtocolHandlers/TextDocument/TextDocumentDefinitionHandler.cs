using System;
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
					if (traitInfo.TraitName != null)
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
					if (traitInfo.TraitName != null)
					{
						var fieldInfo = traitInfo.TraitPropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.Key);
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
						return modSymbols.ActorDefinitions[cursorTarget.TargetString].Select(x => x.Location.ToLspLocation(x.Name.Length));

					return Enumerable.Empty<Location>();
				}

				case 2:
				{
					var traitName = cursorTarget.TargetNode.ParentNode.Key.Split('@')[0];
					var traitInfoName = $"{traitName}Info";

					// Using .First() is not great but we have no way to differentiate between traits of the same name
					// until the server learns the concept of a mod and loaded assemblies.
					var traitInfo = codeSymbols.TraitInfos[traitInfoName].FirstOrDefault();
					if (traitInfo.TraitName != null)
					{
						var fieldInfo = traitInfo.TraitPropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.Key);
						if (fieldInfo.Name != null)
						{
							if (fieldInfo.OtherAttributes.Any(x => x.Name == "ActorReference"))
								return modSymbols.ActorDefinitions[cursorTarget.TargetString].Select(x => x.Location.ToLspLocation(x.Name.Length));

							if (fieldInfo.OtherAttributes.Any(x => x.Name == "WeaponReference"))
								return modSymbols.WeaponDefinitions[cursorTarget.TargetString].Select(x => x.Location.ToLspLocation(x.Name.Length));

							if (fieldInfo.OtherAttributes.Any(x => x.Name == "GrantedConditionReference"))
								return modSymbols.ConditionDefinitions[cursorTarget.TargetString].Select(x => x.Location.ToLspLocation(x.Name.Length));

							if (fieldInfo.OtherAttributes.Any(x => x.Name == "ConsumedConditionReference"))
								return modSymbols.ConditionDefinitions[cursorTarget.TargetString].Select(x => x.Location.ToLspLocation(x.Name.Length));

							if (fieldInfo.OtherAttributes.Any(x => x.Name == "CursorReference"))
								return modSymbols.CursorDefinitions[cursorTarget.TargetString].Select(x => x.Location.ToLspLocation(x.Name.Length));
						}

						return new[] { fieldInfo.Location.ToLspLocation(cursorTarget.TargetString.Length) };
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
					return Enumerable.Empty<Location>();

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
					if (cursorTarget.TargetNode.Key is "Rules" or "Sequences" or "ModelSequences" or "Weapons" or "Voices" or "Music" or "Notifications")
					{
						if (cursorTarget.TargetString.Contains('|'))
						{
							var resolvedFile = OpenRaFolderUtils.ResolveFilePath(cursorTarget.TargetString, (cursorTarget.ModId, symbolCache[cursorTarget.ModId].ModFolder));
							if (File.Exists(resolvedFile))
							{
								return new[]
								{
									new Location
									{
										Uri = new Uri(resolvedFile).ToString(),
										Range = new LspTypes.Range
										{
											Start = new Position(0, 0),
											End = new Position(0, 0)
										}
									}
								};
							}
						}
						else
						{
							var targetPath = cursorTarget.TargetStart.FilePath.Replace("file:///", string.Empty).Replace("%3A", ":");
							var mapFolder = Path.GetDirectoryName(targetPath);
							var filePath = Path.Combine(mapFolder, cursorTarget.TargetString);
							if (File.Exists(filePath))
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
						}
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

		#endregion
	}
}
