using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;
using Oraide.LanguageServer.Caching;
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
					{
						Console.Error.WriteLine("<-- TextDocument-Hover");
						Console.Error.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(positionParams));
					}

					if (TryGetCursorTarget(positionParams, out var target))
					{
						var range = new Range
						{
							Start = new Position((uint)target.TargetStart.LineNumber, (uint)target.TargetStart.CharacterPosition),
							End = new Position((uint)target.TargetEnd.LineNumber, (uint)target.TargetEnd.CharacterPosition)
						};

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
									else
									{
										// Taken from TryGetTargetCodeHoverInfo
										var prop = weaponInfo.WeaponPropertyInfos.FirstOrDefault(x => x.PropertyName == target.TargetString);
										var content = "```csharp\n" +
										          $"{prop.PropertyName} ({prop.PropertyType})" +
										          $"\n```\n" +
										          $"{prop.Description}\n\nDefault value: {prop.DefaultValue}";

										return HoverFromHoverInfo(content, range);
									}
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
										// Taken from TryGetTargetCodeHoverInfo
										var projectileInfo = weaponInfo.ProjectileInfos.FirstOrDefault(x => x.Name == target.TargetString);
										if (projectileInfo.Name != null)
										{
											var content = "```csharp\n" +
											              $"class {projectileInfo.Name}" +
											              $"\n```\n" +
											              $"{projectileInfo.Description}\n\n" +
											              "https://docs.openra.net/en/latest/release/weapons/#" + $"{projectileInfo.Name.ToLower()}";

											return HoverFromHoverInfo(content, range);
										}
									}
									else if (nodeKey == "Warhead" || nodeKey.StartsWith("Warhead@"))
									{
										var warheadInfo = weaponInfo.WarheadInfos.FirstOrDefault(x => x.Name == $"{target.TargetString}Warhead");
										if (warheadInfo.Name != null)
										{
											// Taken from TryGetTargetCodeHoverInfo
											var content = "```csharp\n" +
											              $"class {warheadInfo.Name}" +
											              $"\n```\n" +
											              $"{warheadInfo.Description}\n\n" +
														  "https://docs.openra.net/en/latest/release/weapons/#" + $"{warheadInfo.Name.ToLower()}";

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
										// Taken from TryGetTargetCodeHoverInfo
										var prop = projectileInfo.PropertyInfos.FirstOrDefault(x => x.PropertyName == target.TargetString);
										var content = "```csharp\n" +
										          $"{prop.PropertyName} ({prop.PropertyType})" +
										          $"\n```\n" +
										          $"{prop.Description}\n\nDefault value: {prop.DefaultValue}";

										return HoverFromHoverInfo(content, range);
									}
								}
								else if (parentNode.Key == "Warhead" || parentNode.Key.StartsWith("Warhead@"))
								{
									var warheadInfo = weaponInfo.WarheadInfos.FirstOrDefault(x => x.Name == $"{target.TargetNode.ParentNode.Value}Warhead");
									if (warheadInfo.Name != null)
									{
										// Taken from TryGetTargetCodeHoverInfo
										var prop = warheadInfo.PropertyInfos.FirstOrDefault(x => x.PropertyName == target.TargetString);
										var content = "```csharp\n" +
										              $"{prop.PropertyName} ({prop.PropertyType})" +
										              $"\n```\n" +
										              $"{prop.Description}\n\nDefault value: {prop.DefaultValue}";

										return HoverFromHoverInfo(content, range);
									}
								}
							}
						}

						if (TryGetTargetValueHoverInfo(target, out var valueHoverInfo))
							return HoverFromHoverInfo(valueHoverInfo.Content, valueHoverInfo.Range);
					}
				}
				catch (Exception)
				{
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

			string content;
			if (traitInfo.TraitName == target.TargetString)
			{
				content = "```csharp\n" +
				          $"class {traitInfo.TraitInfoName}" +
				          $"\n```\n" +
						  $"{traitInfo.TraitDescription}\n\n" +
				          "https://docs.openra.net/en/latest/release/traits/#" + $"{traitInfo.TraitName.ToLower()}";
			}
			else
			{
				var prop = traitInfo.TraitPropertyInfos.FirstOrDefault(x => x.PropertyName == target.TargetString);
				content = "```csharp\n" +
				          $"{prop.PropertyName} ({prop.PropertyType})" +
				          $"\n```\n" +
						  $"{prop.Description}\n\nDefault value: {prop.DefaultValue}";
			}

			if (string.IsNullOrWhiteSpace(content))
			{
				hoverInfo = (null, null);
				return false;
			}

			hoverInfo = (
				content,
				new Range
				{
					Start = new Position((uint)target.TargetStart.LineNumber, (uint)target.TargetStart.CharacterPosition),
					End = new Position((uint)target.TargetEnd.LineNumber, (uint)target.TargetEnd.CharacterPosition)
				});

			return true;
		}

		private bool TryGetTargetYamlHoverInfo(CursorTarget target, out (string Content, Range Range) hoverInfo)
		{
			if (symbolCache[target.ModId].ActorDefinitions.Any(x => x.Key == target.TargetString))
			{
				hoverInfo = (
					$"```csharp\nActor \"{target.TargetString}\"\n```", new Range
					{
						Start = new Position((uint)target.TargetStart.LineNumber, (uint)target.TargetStart.CharacterPosition),
						End = new Position((uint)target.TargetEnd.LineNumber, (uint)target.TargetEnd.CharacterPosition)
					});

				return true;
			}

			if (symbolCache[target.ModId].WeaponDefinitions.Any(x => x.Key == target.TargetString))
			{
				hoverInfo = (
					$"```csharp\nWeapon \"{target.TargetString}\"\n```", new Range
					{
						Start = new Position((uint)target.TargetStart.LineNumber, (uint)target.TargetStart.CharacterPosition),
						End = new Position((uint)target.TargetEnd.LineNumber, (uint)target.TargetEnd.CharacterPosition)
					});

				return true;
			}

			if (symbolCache[target.ModId].ConditionDefinitions.Any(x => x.Key == target.TargetString))
			{
				hoverInfo = (
					$"```csharp\nCondition \"{target.TargetString}\"\n```", new Range
					{
						Start = new Position((uint)target.TargetStart.LineNumber, (uint)target.TargetStart.CharacterPosition),
						End = new Position((uint)target.TargetEnd.LineNumber, (uint)target.TargetEnd.CharacterPosition)
					});

				return true;
			}

			hoverInfo = (null, null);
			return false;
		}

		private bool TryGetTargetValueHoverInfo(CursorTarget target, out (string Content, Range Range) hoverInfo)
		{
			if (target.TargetType == "key")
			{
				if (target.TargetString == "Inherits")
				{
					hoverInfo = ($"Inherits (and possibly overwrites) rules from {target.TargetNode.Value ?? (target.FileType == FileType.Rules ? "an actor" : "a weapon")}", new Range
					{
						Start = new Position((uint)target.TargetStart.LineNumber, (uint)target.TargetStart.CharacterPosition),
						End = new Position((uint)target.TargetEnd.LineNumber, (uint)target.TargetEnd.CharacterPosition)
					});

					return true;
				}
			}
			else if (target.TargetType == "value")
			{
				if (Regex.IsMatch(target.TargetString, "[0-9]+c[0-9]+", RegexOptions.Compiled))
				{
					var parts = target.TargetString.Split('c');
					hoverInfo = ($"Range in world distance units equal to {parts[0]} cells and {parts[1]} distance units (where 1 cell has 1024 units)", new Range
					{
						Start = new Position((uint)target.TargetStart.LineNumber, (uint)target.TargetStart.CharacterPosition),
						End = new Position((uint)target.TargetEnd.LineNumber, (uint)target.TargetEnd.CharacterPosition)
					});

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
