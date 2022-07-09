using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LspTypes;
using Oraide.Core;
using Oraide.Core.Entities;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Caching;

namespace Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers
{
	public abstract class BaseRpcMessageHandler : IRpcMessageHandler
	{
		protected static readonly object LockObject = new();

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
			// HACK HACK HACK!!!
			// For whatever reason we receive the file URI borked - looks to be encoded for JSON, but the deserialization doesn't fix it.
			// No idea if this is an issue with VSCode or the LSP library used as there are currently no clients for other text editors.
			var incomingFileUriString = OpenRaFolderUtils.NormalizeFileUriString(positionParams.TextDocument.Uri);

			TryGetModId(incomingFileUriString, out var modId);
			var fileUri = new Uri(incomingFileUriString);
			var targetLineIndex = (int)positionParams.Position.Line;
			var targetCharacterIndex = (int)positionParams.Position.Character;

			// Determine file type.
			var modManifest = symbolCache[modId].ModManifest;
			var fileName = fileUri.AbsoluteUri.Split($"mods/{modId}/")[1];
			var fileReference = $"{modId}|{fileName}";
			var filePath = fileUri.AbsolutePath;

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

			if (!openFileCache.ContainsFile(fileUri.AbsoluteUri))
			{
				target = default;
				return false;
			}

			var (fileNodes, flattenedNodes, fileLines) = openFileCache[fileUri.AbsoluteUri];

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

			// Workaround for when the file is in the SupportDir.
			if (string.IsNullOrEmpty(match))
				match = Regex.Match(fileUri, "(\\/maps\\/[^\\/]*\\/)").Value;

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

		protected string NormalizeFilePath(string filePath)
		{
			// Because VSCode sends us weird partially-url-encoded file paths.
			return System.Web.HttpUtility.UrlDecode(filePath);
		}

		protected ActorTraitDefinition[] GetInheritedTraitNodes(CursorTarget cursorTarget)
		{
			var actor = symbolCache[cursorTarget.ModId].ModSymbols.ActorDefinitions[cursorTarget.TargetNode.ParentNode.Key];
			// var kor = GetInheritedActorsRecursively(cursorTarget.TargetNode.ParentNode, cursorTarget).ToArray();
			//
			// var modSymbols = symbolCache.ModSymbols[cursorTarget.ModId].ModSymbols;
			// var inheritedNodes = cursorTarget.TargetNode.ParentNode.ChildNodes
			// 	.Where(x => x.Key == "Inherits");
			// 	// .SelectMany(x => modSymbols.ActorDefinitions[x.Value]);
			//
			// foreach (var node in inheritedNodes)
			// {
			//
			// }
			//
			// var traitName = cursorTarget.TargetString[0] == '-'
			// 	? cursorTarget.TargetString.Substring(1)
			// 	: cursorTarget.TargetString;
			//
			// return inheritedDefinitions
			// 	.SelectMany(x => x.Traits.Where(y => y.Name == traitName))
			// 	.ToArray();
			return null;
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

			startIndex = targetLine.IndexOf(targetString, Math.Max(targetCharacterIndex - targetString.Length, 0), StringComparison.InvariantCulture);
			endIndex = startIndex + targetString.Length;
			return true;
		}

		// IEnumerable<ActorDefinition> GetInheritedActorDefinitionssRecursively(ActorDefinition actor, CursorTarget cursorTarget)
		// {
		// 	if (node?.ChildNodes == null)
		// 		yield break;
		//
		// 	foreach (var childNode in node.ChildNodes.Where(x => x.Key == "Inherits"))
		// 	{
		// 		yield return childNode;
		//
		// 		var inheritedNode = symbolCache.ModSymbols[cursorTarget.ModId].ModSymbols.ActorDefinitions[childNode.Value]
		// 			.Select(x => openFileCache[x.Location.FilePath].YamlNodes.FirstOrDefault(y => y.Key == childNode.Value));
		// 		var fileNodes = openFileCache[cursorTarget.TargetStart.FilePath].YamlNodes;
		// 		foreach (var yamlNode in GetInheritedActorsRecursively(childNode, cursorTarget))
		// 			yield return yamlNode;
		// 	}
		// }
	}
}
