using System;
using System.Linq;
using System.Text.RegularExpressions;
using LspTypes;
using Oraide.Core.Entities.Csharp;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;
using Oraide.LanguageServer.Caching;
using Oraide.LanguageServer.Extensions;
using Range = LspTypes.Range;

namespace Oraide.LanguageServer.LanguageServerProtocolHandlers.TextDocument
{
	public class TextDocumentHoverHandler : BaseRpcMessageHandler
	{
		Range range;
		WeaponInfo weaponInfo;
		ModSymbols modSymbols;

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
			modSymbols = symbolCache[cursorTarget.ModId];
			weaponInfo = symbolCache[cursorTarget.ModId].WeaponInfo;
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

					return null;
				}

				case 1:
				{
					if (cursorTarget.TargetString == "Inherits")
						return HoverFromHoverInfo($"Inherits (and possibly overwrites) rules from actor {cursorTarget.TargetNode.Value}", range);

					var traitInfoName = $"{cursorTarget.TargetString}Info";
					if (modSymbols.TraitInfos.Contains(traitInfoName))
					{
						// Using .First() is not great but we have no way to differentiate between traits of the same name
						// until the server learns the concept of a mod and loaded assemblies.
						var traitInfo = modSymbols.TraitInfos[traitInfoName].First();
						var content = traitInfo.ToMarkdownInfoString() + "\n\n" + "https://docs.openra.net/en/latest/release/traits/#" + $"{traitInfo.TraitName.ToLower()}";
						return HoverFromHoverInfo(content, range);
					}

					return null;
				}

				case 2:
				{
					var traitInfoName = $"{cursorTarget.TargetNode.ParentNode.Key.Split("@")[0]}Info";
					if (modSymbols.TraitInfos.Contains(traitInfoName))
					{
						// Using .First() is not great but we have no way to differentiate between traits of the same name
						// until the server learns the concept of a mod and loaded assemblies.
						var traitInfo = modSymbols.TraitInfos[traitInfoName].First();
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
						if (modSymbols.ActorDefinitions.Contains(cursorTarget.TargetString))
							return HoverFromHoverInfo($"```csharp\nActor \"{cursorTarget.TargetString}\"\n```", range);

					return null;
				}

				case 2:
				{
					var traitInfoName = $"{cursorTarget.TargetNode.ParentNode.Key.Split("@")[0]}Info";
					if (modSymbols.TraitInfos.Contains(traitInfoName))
					{
						// Using .First() is not great but we have no way to differentiate between traits of the same name
						// until the server learns the concept of a mod and loaded assemblies.
						var traitInfo = modSymbols.TraitInfos[traitInfoName].First();
						var fieldInfo = traitInfo.TraitPropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.Key);
						if (fieldInfo.Name != null)
						{
							if (fieldInfo.OtherAttributes.Any(x => x.Name == "ActorReference") && modSymbols.ActorDefinitions.Contains(cursorTarget.TargetString))
								return HoverFromHoverInfo($"```csharp\nActor \"{cursorTarget.TargetString}\"\n```", range);

							if (fieldInfo.OtherAttributes.Any(x => x.Name == "WeaponReference") && modSymbols.WeaponDefinitions.Contains(cursorTarget.TargetString))
								return HoverFromHoverInfo($"```csharp\nWeapon \"{cursorTarget.TargetString}\"\n```", range);

							if (fieldInfo.OtherAttributes.Any(x => x.Name == "GrantedConditionReference") && modSymbols.ConditionDefinitions.Contains(cursorTarget.TargetString))
								return HoverFromHoverInfo($"```csharp\nCondition \"{cursorTarget.TargetString}\"\n```", range);

							if (fieldInfo.OtherAttributes.Any(x => x.Name == "ConsumedConditionReference") && modSymbols.ConditionDefinitions.Contains(cursorTarget.TargetString))
								return HoverFromHoverInfo($"```csharp\nCondition \"{cursorTarget.TargetString}\"\n```", range);

							if (fieldInfo.OtherAttributes.Any(x => x.Name == "CursorReference") && modSymbols.CursorDefinitions.Contains(cursorTarget.TargetString))
							{
								var cursor = modSymbols.CursorDefinitions[cursorTarget.TargetString].First();
								return HoverFromHoverInfo(cursor.ToMarkdownInfoString(), range);
							}
						}

						// Show explanation for world range value.
						if(Regex.IsMatch(cursorTarget.TargetString, "[0-9]+c[0-9]+", RegexOptions.Compiled))
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

					return null;
				}

				case 1:
				{
					if (cursorTarget.TargetString == "Inherits")
						return HoverFromHoverInfo($"Inherits (and possibly overwrites) rules from weapon {cursorTarget.TargetNode.Value}", range);

					if (cursorTarget.TargetString == "Warhead" || cursorTarget.TargetString.StartsWith("Warhead@"))
						return HoverFromHoverInfo("Warhead used by this weapon.", range);

					// Maybe this is a property of WeaponInfo.
					var prop = weaponInfo.WeaponPropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetString);
					if (prop.Name != null)
						return HoverFromHoverInfo(prop.ToMarkdownInfoString(), range);

					return null;
				}

				case 2:
				{
					var parentNode = cursorTarget.TargetNode.ParentNode;
					if (parentNode.Key == "Projectile")
					{
						var projectileInfo = weaponInfo.ProjectileInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.ParentNode.Value);
						if (projectileInfo.Name != null)
						{
							var prop = projectileInfo.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetString);
							if (prop.Name != null)
								return HoverFromHoverInfo(prop.ToMarkdownInfoString(), range);
						}
					}
					else if (parentNode.Key == "Warhead" || parentNode.Key.StartsWith("Warhead@"))
					{
						var warheadInfo = weaponInfo.WarheadInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.ParentNode.Value);
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
						if (symbolCache[cursorTarget.ModId].WeaponDefinitions.Any(x => x.Key == cursorTarget.TargetString))
							return HoverFromHoverInfo($"```csharp\nWeapon \"{cursorTarget.TargetString}\"\n```", range);
					}
					else if (nodeKey == "Projectile")
					{
						var projectileInfo = weaponInfo.ProjectileInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetString);
						if (projectileInfo.Name != null)
						{
							var content = projectileInfo.ToMarkdownInfoString() +
							              "\n\n" + "https://docs.openra.net/en/latest/release/weapons/#" + $"{projectileInfo.Name.ToLower()}";

							return HoverFromHoverInfo(content, range);
						}
					}
					else if (nodeKey == "Warhead" || nodeKey.StartsWith("Warhead@"))
					{
						var warheadInfo = weaponInfo.WarheadInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetString);
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
