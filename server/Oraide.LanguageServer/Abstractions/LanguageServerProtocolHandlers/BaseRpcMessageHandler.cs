using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LspTypes;
using Oraide.Core.Entities;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Caching;

namespace Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers
{
	public abstract class BaseRpcMessageHandler : IRpcMessageHandler
	{
		protected static readonly object LockObject = new object();

		protected readonly bool trace = true;
		protected readonly SymbolCache symbolCache;
		protected readonly OpenFileCache openFileCache;

		protected BaseRpcMessageHandler(SymbolCache symbolCache, OpenFileCache openFileCache)
		{
			this.symbolCache = symbolCache;
			this.openFileCache = openFileCache;
		}

		protected virtual bool TryGetCursorTarget(TextDocumentPositionParams positionParams, out CursorTarget target)
		{
			TryGetModId(positionParams.TextDocument.Uri, out var modId);
			var fileUri = positionParams.TextDocument.Uri;
			var targetLineIndex = (int)positionParams.Position.Line;
			var targetCharacterIndex = (int)positionParams.Position.Character;

			// Determine file type.
			var modManifest = symbolCache[modId].ModManifest;
			var fileName = fileUri.Split($"mods/{modId}/")[1];
			var fileReference = $"{modId}|{fileName}";
			var filePath = fileUri.Replace("file:///", string.Empty).Replace("%3A", ":");

			var fileType = FileType.Unknown;
			if (modManifest.RulesFiles.Contains(fileReference))
				fileType = FileType.Rules;
			else if (modManifest.WeaponsFiles.Contains(fileReference))
				fileType = FileType.Weapons;
			else if (modManifest.CursorsFiles.Contains(fileReference))
				fileType = FileType.Cursors;
			else if (Path.GetFileName(filePath) == "map.yaml" && symbolCache[modId].Maps.Any(x => x.MapFolder == Path.GetDirectoryName(filePath)))
				fileType = FileType.MapFile;
			else if (symbolCache[modId].Maps.Any(x => x.RulesFiles.Contains(fileReference)))
				fileType = FileType.MapRules;
			else if (symbolCache[modId].Maps.Any(x => x.WeaponsFiles.Contains(fileReference)))
				fileType = FileType.MapWeapons;

			if (!openFileCache.ContainsFile(fileUri))
			{
				target = default;
				return false;
			}

			var (fileNodes, flattenedNodes, fileLines) = openFileCache[fileUri];

			var targetLine = fileLines[targetLineIndex];
			var pre = targetLine.Substring(0, targetCharacterIndex);
			var post = targetLine.Substring(targetCharacterIndex);

			if ((string.IsNullOrWhiteSpace(pre) && (post[0] == '\t' || post[0] == ' '))
			    || string.IsNullOrWhiteSpace(post))
			{
				target = default;
				return false;
			}

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
					sourceString = targetNode.Key.Split('@')[1];
				}
				else
				{
					targetType = "key";
					sourceString = targetNode.Key.Split('@')[0];
				}
			}

			if (!TryGetTargetString(targetLine, targetCharacterIndex, sourceString, out var targetString, out var startIndex, out var endIndex))
			{
				target = default;
				return false;
			}

			// idk at this point
			if (string.IsNullOrWhiteSpace(targetString))
				targetString = sourceString;

			TryGetTargetStringIndentation(targetNode, out var indentation);
			target = new CursorTarget(modId, fileType, fileReference, targetNode, targetType, targetString,
				new MemberLocation(fileUri, targetLineIndex, startIndex),
				new MemberLocation(fileUri, targetLineIndex, endIndex), indentation);

			return true;
		}

		protected virtual void Initialize(CursorTarget cursorTarget) { }

		#region CursorTarget handlers

		protected virtual object HandlePositionalRequest(TextDocumentPositionParams positionParams)
		{
			if (!TryGetCursorTarget(positionParams, out var cursorTarget))
				return null;

			Initialize(cursorTarget);

			return cursorTarget.FileType switch
			{
				FileType.Rules => HandleRulesFile(cursorTarget),
				FileType.Weapons => HandleWeaponFile(cursorTarget),
				FileType.Cursors => HandleCursorsFile(cursorTarget),
				FileType.MapFile => HandleMapFile(cursorTarget),
				FileType.MapRules => HandleRulesFile(cursorTarget),
				FileType.MapWeapons => HandleWeaponFile(cursorTarget),
				_ => null
			};
		}

		protected virtual object HandleRulesFile(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetType switch
			{
				"key" => HandleRulesKey(cursorTarget),
				"value" => HandleRulesValue(cursorTarget),
				_ => null
			};
		}

		protected virtual object HandleWeaponFile(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetType switch
			{
				"key" => HandleWeaponKey(cursorTarget),
				"value" => HandleWeaponValue(cursorTarget),
				_ => null
			};
		}

		protected virtual object HandleCursorsFile(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetType switch
			{
				"key" => HandleCursorsKey(cursorTarget),
				"value" => HandleCursorsValue(cursorTarget),
				_ => null
			};
		}

		protected virtual object HandleMapFile(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetType switch
			{
				"key" => HandleMapFileKey(cursorTarget),
				"value" => HandleMapFileValue(cursorTarget),
				_ => null
			};
		}

		protected virtual object HandleRulesKey(CursorTarget cursorTarget) { return null; }

		protected virtual object HandleRulesValue(CursorTarget cursorTarget) { return null; }

		protected virtual object HandleWeaponKey(CursorTarget cursorTarget) { return null; }

		protected virtual object HandleWeaponValue(CursorTarget cursorTarget) { return null; }

		protected virtual object HandleCursorsKey(CursorTarget cursorTarget) { return null; }

		protected virtual object HandleCursorsValue(CursorTarget cursorTarget) { return null; }

		protected virtual object HandleMapFileKey(CursorTarget cursorTarget) { return null; }

		protected virtual object HandleMapFileValue(CursorTarget cursorTarget) { return null; }

		#endregion

		protected bool TryGetModId(string fileUri, out string modId)
		{
			var match = Regex.Match(fileUri, "(\\/mods\\/[^\\/]*\\/)").Value;
			modId = match.Split('/')[2];
			return true;
		}

		protected bool TryGetTargetStringIndentation(YamlNode yamlNode, out int indentation)
		{
			indentation = 0;
			var node = yamlNode;
			while (node.ParentNode != null)
			{
				node = node.ParentNode;
				indentation++;
			}

			return true;
		}

		bool TryGetTargetString(string targetLine, int targetCharacterIndex, string sourceString, out string targetString, out int startIndex, out int endIndex)
		{
			targetString = string.Empty;
			startIndex = 0;
			endIndex = 1;

			var hasReached = false;
			while (endIndex < sourceString.Length)
			{
				if (endIndex == targetCharacterIndex - targetLine.LastIndexOf(sourceString, StringComparison.InvariantCulture))
					hasReached = true;

				if (sourceString[endIndex] == ',' || endIndex == sourceString.Length - 1)
				{
					if (!hasReached)
						startIndex = endIndex;
					else
					{
						targetString = sourceString.Substring(startIndex, endIndex - startIndex + 1).Trim(' ', '\t', ',', '!');
						Console.Error.WriteLine(targetString);
						break;
					}
				}

				endIndex++;
			}

			startIndex = targetLine.IndexOf(targetString, StringComparison.InvariantCulture);
			endIndex = startIndex + targetString.Length;
			return true;
		}
	}
}
