using System;
using System.Linq;
using System.Text.RegularExpressions;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;
using Oraide.LanguageServer.Caching;
using Oraide.LanguageServer.Extensions;
using Range = LspTypes.Range;

namespace Oraide.LanguageServer.LanguageServerProtocolHandlers.TextDocument
{
	public class TextDocumentHoverHandler : BaseRpcMessageHandler
	{
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

					if (TryGetCursorTarget(positionParams, out var target))
					{
						var range = target.ToRange();

						if (target.FileType == FileType.Rules)
						{
							if (target.TargetType == "key"
							    && (target.TargetNodeIndentation == 1 || target.TargetNodeIndentation == 2)
							    && TryGetTargetCodeHoverInfo(target, out var codeHoverInfo))
								return HoverFromHoverInfo(codeHoverInfo.Content, codeHoverInfo.Range);

							if ((target.TargetType == "value" || target.TargetNodeIndentation == 0)
							    && TryGetTargetYamlHoverInfo(target, out var yamlHoverInfo))
								return HoverFromHoverInfo(yamlHoverInfo.Content, yamlHoverInfo.Range);
						}
						else if (target.FileType == FileType.Weapons)
						{
							var weaponInfo = symbolCache[target.ModId].WeaponInfo;

							if (target.TargetNodeIndentation == 0 && target.TargetType == "key")
								if (TryGetTargetYamlHoverInfo(target, out var yamlHoverInfo))
									return HoverFromHoverInfo(yamlHoverInfo.Content, yamlHoverInfo.Range);

							if (target.TargetNodeIndentation == 1)
							{
								if (target.TargetType == "key")
								{
									if (target.TargetString == "Inherits")
										return HoverFromHoverInfo("Inherits (and possibly overwrites) rules from a weapon", range);

									if (target.TargetString == "Warhead" || target.TargetString.StartsWith("Warhead@"))
										return HoverFromHoverInfo("Warhead used by this weapon.", range);

									// Maybe this is a property of WeaponInfo.
									var prop = weaponInfo.WeaponPropertyInfos.FirstOrDefault(x => x.Name == target.TargetString);
									if (prop.Name != null)
										return HoverFromHoverInfo(prop.ToMarkdownInfoString(), range);
								}
								else if (target.TargetType == "value")
								{
									var nodeKey = target.TargetNode.Key;

									if (nodeKey == "Inherits")
									{
										if (symbolCache[target.ModId].WeaponDefinitions.Any(x => x.Key == target.TargetString))
											return HoverFromHoverInfo($"```csharp\nWeapon \"{target.TargetString}\"\n```", range);
									}
									else if (nodeKey == "Projectile")
									{
										var projectileInfo = weaponInfo.ProjectileInfos.FirstOrDefault(x => x.Name == target.TargetString);
										if (projectileInfo.Name != null)
										{
											var content = projectileInfo.ToMarkdownInfoString() +
											              "\n\n" + "https://docs.openra.net/en/latest/release/weapons/#" + $"{projectileInfo.Name.ToLower()}";

											return HoverFromHoverInfo(content, range);
										}
									}
									else if (nodeKey == "Warhead" || nodeKey.StartsWith("Warhead@"))
									{
										var warheadInfo = weaponInfo.WarheadInfos.FirstOrDefault(x => x.Name == target.TargetString);
										if (warheadInfo.Name != null)
										{
											var content = warheadInfo.ToMarkdownInfoString() +
														  "\n\n" + "https://docs.openra.net/en/latest/release/weapons/#" + $"{warheadInfo.Name.ToLower()}";

											return HoverFromHoverInfo(content, range);
										}
									}
								}
							}
							else if (target.TargetNodeIndentation == 2)
							{
								var parentNode = target.TargetNode.ParentNode;
								if (parentNode.Key == "Projectile")
								{
									var projectileInfo = weaponInfo.ProjectileInfos.FirstOrDefault(x => x.Name == target.TargetNode.ParentNode.Value);
									if (projectileInfo.Name != null)
									{
										var prop = projectileInfo.PropertyInfos.FirstOrDefault(x => x.Name == target.TargetString);
										if (prop.Name != null)
											return HoverFromHoverInfo(prop.ToMarkdownInfoString(), range);
									}
								}
								else if (parentNode.Key == "Warhead" || parentNode.Key.StartsWith("Warhead@"))
								{
									var warheadInfo = weaponInfo.WarheadInfos.FirstOrDefault(x => x.Name == target.TargetNode.ParentNode.Value);
									if (warheadInfo.Name != null)
									{
										var prop = warheadInfo.PropertyInfos.FirstOrDefault(x => x.Name == target.TargetString);
										if (prop.Name != null)
											return HoverFromHoverInfo(prop.ToMarkdownInfoString(), range);
									}
								}
							}
						}

						if (TryGetTargetValueHoverInfo(target, out var valueHoverInfo))
							return HoverFromHoverInfo(valueHoverInfo.Content, valueHoverInfo.Range);
					}
				}
				catch (Exception e)
				{
					Console.Error.WriteLine("EXCEPTION!!!");
					Console.Error.WriteLine(e.ToString());
				}

				return null;
			}
		}

		private bool TryGetTargetCodeHoverInfo(CursorTarget target, out (string Content, Range Range) hoverInfo)
		{
			if (!TryGetCodeMemberLocation(target.TargetNode, target.TargetString, out var traitInfo, out _))
			{
				hoverInfo = (null, null);
				return false;
			}

			var content = string.Empty;
			if (traitInfo.TraitName == target.TargetString)
			{
				content = traitInfo.ToMarkdownInfoString() + "\n\n" + "https://docs.openra.net/en/latest/release/traits/#" + $"{traitInfo.TraitName.ToLower()}";
			}
			else
			{
				var prop = traitInfo.TraitPropertyInfos.FirstOrDefault(x => x.Name == target.TargetString);
				if (prop.Name != null)
					content = prop.ToMarkdownInfoString();
			}

			if (string.IsNullOrWhiteSpace(content))
			{
				hoverInfo = (null, null);
				return false;
			}

			hoverInfo = (content, target.ToRange());
			return true;
		}

		private bool TryGetTargetYamlHoverInfo(CursorTarget target, out (string Content, Range Range) hoverInfo)
		{
			var symbolType = string.Empty;

			if (symbolCache[target.ModId].ActorDefinitions.Any(x => x.Key == target.TargetString))
				symbolType = "Actor";

			if (symbolCache[target.ModId].WeaponDefinitions.Any(x => x.Key == target.TargetString))
				symbolType = "Weapon";

			if (symbolCache[target.ModId].ConditionDefinitions.Any(x => x.Key == target.TargetString))
				symbolType = "Condition";

			if (string.IsNullOrWhiteSpace(symbolType))
			{
				hoverInfo = (null, null);
				return false;
			}

			hoverInfo = ($"```csharp\n{symbolType} \"{target.TargetString}\"\n```", target.ToRange());
			return true;
		}

		private bool TryGetTargetValueHoverInfo(CursorTarget target, out (string Content, Range Range) hoverInfo)
		{
			var range = target.ToRange();
			if (target.TargetType == "key")
			{
				if (target.TargetString == "Inherits")
				{
					hoverInfo = ($"Inherits (and possibly overwrites) rules from {target.TargetNode.Value ?? (target.FileType == FileType.Rules ? "an actor" : "a weapon")}", range);
					return true;
				}
			}
			else if (target.TargetType == "value")
			{
				if (Regex.IsMatch(target.TargetString, "[0-9]+c[0-9]+", RegexOptions.Compiled))
				{
					var parts = target.TargetString.Split('c');
					hoverInfo = ($"Range in world distance units equal to {parts[0]} cells and {parts[1]} distance units (where 1 cell has 1024 units)", range);

					return true;
				}
			}

			hoverInfo = (null, null);
			return false;
		}

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
