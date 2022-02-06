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

					// TODO:
					// if (TryGetTargetValueHoverInfo(target, out var valueHoverInfo))
					// 	return HoverFromHoverInfo(valueHoverInfo.Content, valueHoverInfo.Range);
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
			weaponInfo = symbolCache[cursorTarget.ModId].WeaponInfo;
		}

		#region CursorTarget handlers

		// TODO:
		protected override Hover HandleRulesKey(CursorTarget cursorTarget)
		{
			if (cursorTarget.TargetType == "key"
			    && (cursorTarget.TargetNodeIndentation == 1 || cursorTarget.TargetNodeIndentation == 2)
			    && TryGetTargetCodeHoverInfo(cursorTarget, out var codeHoverInfo))
				return HoverFromHoverInfo(codeHoverInfo.Content, codeHoverInfo.Range);

			return null;
		}

		// TODO:
		protected override Hover HandleRulesValue(CursorTarget cursorTarget)
		{
			if ((cursorTarget.TargetType == "value" || cursorTarget.TargetNodeIndentation == 0)
			    && TryGetTargetYamlHoverInfo(cursorTarget, out var yamlHoverInfo))
				return HoverFromHoverInfo(yamlHoverInfo.Content, yamlHoverInfo.Range);

			return null;
		}

		protected override Hover HandleWeaponKey(CursorTarget cursorTarget)
		{
			switch (cursorTarget.TargetNodeIndentation)
			{
				case 0:
					if (TryGetTargetYamlHoverInfo(cursorTarget, out var yamlHoverInfo))
						return HoverFromHoverInfo(yamlHoverInfo.Content, yamlHoverInfo.Range);

					return null;

				case 1:
				{
					if (cursorTarget.TargetString == "Inherits")
						return HoverFromHoverInfo("Inherits (and possibly overwrites) rules from a weapon", range);

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

					return null;
				}

				case 2:
					return null;

				default:
					return null;
			}
		}

		#endregion

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
