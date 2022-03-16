using System;
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
			var filePath = positionParams.TextDocument.Uri;
			var targetLineIndex = (int)positionParams.Position.Line;
			var targetCharacterIndex = (int)positionParams.Position.Character;

			// Determine file type.
			var fileType = FileType.Unknown;
			if (filePath.Contains("/rules/") || (filePath.Contains("/maps/") && !filePath.EndsWith("map.yaml")))
				fileType = FileType.Rules;
			else if (filePath.Contains("/weapons/"))
				fileType = FileType.Weapons;
			else if (filePath.Contains("cursor")) // TODO: These checks are getting ridiculous.
				fileType = FileType.Cursors;

			if (!openFileCache.ContainsFile(filePath))
			{
				target = default;
				return false;
			}

			var (fileNodes, flattenedNodes, fileLines) = openFileCache[filePath];

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

			TryGetModId(positionParams.TextDocument.Uri, out var modId);
			TryGetTargetStringIndentation(targetNode, out var indentation);
			target = new CursorTarget(modId, fileType, targetNode, targetType, targetString,
				new MemberLocation(filePath, targetLineIndex, startIndex),
				new MemberLocation(filePath, targetLineIndex, endIndex), indentation);

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

		protected virtual object HandleRulesKey(CursorTarget cursorTarget) { return null; }

		protected virtual object HandleRulesValue(CursorTarget cursorTarget) { return null; }

		protected virtual object HandleWeaponKey(CursorTarget cursorTarget) { return null; }

		protected virtual object HandleWeaponValue(CursorTarget cursorTarget) { return null; }

		protected virtual object HandleCursorsKey(CursorTarget cursorTarget) { return null; }

		protected virtual object HandleCursorsValue(CursorTarget cursorTarget) { return null; }

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
						break;
					}
				}

				endIndex++;
			}

			startIndex = targetLine.IndexOf(targetString, targetCharacterIndex - targetString.Length, StringComparison.InvariantCulture);
			endIndex = startIndex + targetString.Length;
			return true;
		}
	}
}
