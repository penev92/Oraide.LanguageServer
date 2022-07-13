using System.Collections.Generic;
using System.IO;
using System.Linq;
using Oraide.Core;
using Oraide.Core.Entities;
using Oraide.Core.Entities.MiniYaml;

namespace Oraide.MiniYaml.YamlParsers
{
	public static class OpenRAMiniYamlParser
	{
		public static IEnumerable<YamlNode> ReadModFile(string modFolder)
		{
			return OpenRA.MiniYamlParser.MiniYamlLoader.FromFile(Path.Combine(modFolder, "mod.yaml")).Select(x => x.ToYamlNode());
		}

		public static IEnumerable<YamlNode> ReadMapFile(string mapFolder)
		{
			return OpenRA.MiniYamlParser.MiniYamlLoader.FromFile(Path.Combine(mapFolder, "map.yaml")).Select(x => x.ToYamlNode());
		}

		public static ILookup<string, ActorDefinition> GetActorDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods)
		{
			var actorDefinitions = new List<ActorDefinition>();
			var yamlNodes = YamlNodesFromReferencedFiles(referencedFiles, mods);
			foreach (var node in yamlNodes)
			{
				var location = new MemberLocation(node.Location.FileUri, node.Location.LineNumber, node.Location.CharacterPosition);

				var actorTraits = node.ChildNodes == null
					? Enumerable.Empty<ActorTraitDefinition>()
					: node.ChildNodes.Select(x =>
						new ActorTraitDefinition(x.Key, x.Value, new MemberLocation(x.Location.FileUri, x.Location.LineNumber, 1))); // HACK HACK HACK: Until the YAML Loader learns about character positions, we hardcode 1 here (since this is all for traits on actor definitions).

				var tooltipTrait = node.ChildNodes?.FirstOrDefault(x => x.Key.EndsWith("Tooltip"));
				var displayName = tooltipTrait?.ChildNodes?.FirstOrDefault(x => x.Key == "Name")?.Value;
				actorDefinitions.Add(new ActorDefinition(node.Key, displayName, location, actorTraits.ToList()));
			}

			return actorDefinitions.ToLookup(n => n.Name, m => m);
		}

		public static ILookup<string, WeaponDefinition> GetWeaponDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods)
		{
			var weaponDefinitions = new List<WeaponDefinition>();
			var yamlNodes = YamlNodesFromReferencedFiles(referencedFiles, mods);
			foreach (var node in yamlNodes)
			{
				var location = new MemberLocation(node.Location.FileUri, node.Location.LineNumber, node.Location.CharacterPosition);

				var projectile = node.ChildNodes?.FirstOrDefault(x => x.Key == "Projectile");
				var warheads = node.ChildNodes?.Where(x => x.Key == "Warhead" || x.Key.StartsWith("Warhead@")) ?? Enumerable.Empty<YamlNode>();

				weaponDefinitions.Add(new WeaponDefinition(node.Key,
					projectile == null ? default : new WeaponProjectileDefinition(projectile.Value, new MemberLocation(projectile.Location.FileUri, projectile.Location.LineNumber, projectile.Location.CharacterPosition)),
					warheads.Select(x => new WeaponWarheadDefinition(x.Value, new MemberLocation(x.Location.FileUri, x.Location.LineNumber, x.Location.CharacterPosition))).ToArray(),
					location));
			}

			return weaponDefinitions.ToLookup(n => n.Name, m => m);
		}

		public static ILookup<string, ConditionDefinition> GetConditionDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods)
		{
			var result = new List<ConditionDefinition>();

			var fileUris = referencedFiles.Select(fileReference => OpenRaFolderUtils.ResolveFilePath(fileReference, mods));
			foreach (var fileUri in fileUris)
			{
				if (fileUri == null)
					continue;

				var nodes = OpenRA.MiniYamlParser.MiniYamlLoader.FromFile(fileUri.LocalPath);
				var yamlNodes = nodes.Select(x => x.ToYamlNode());
				var flattenedNodes = yamlNodes.SelectMany(FlattenChildNodes);
				var conditions = flattenedNodes
					.Where(x => x.Key.EndsWith("Condition") && !string.IsNullOrWhiteSpace(x.Value))
					.Select(x => new ConditionDefinition(x.Value.TrimStart('!'), x.Location));

				result.AddRange(conditions);
			}

			return result.ToLookup(n => n.Name, m => m);
		}

		public static ILookup<string, CursorDefinition> GetCursorDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods)
		{
			var result = new List<CursorDefinition>();

			var fileUris = referencedFiles.Select(fileReference => OpenRaFolderUtils.ResolveFilePath(fileReference, mods));
			foreach (var fileUri in fileUris)
			{
				if (fileUri == null)
					continue;

				var nodes = OpenRA.MiniYamlParser.MiniYamlLoader.FromFile(fileUri.LocalPath);
				var yamlNodes = nodes.SelectMany(x =>
					x.Value.Nodes.SelectMany(y =>
						y.Value.Nodes.Select(z => z.ToYamlNode(y.ToYamlNode()))));

				var cursorDefinitions = yamlNodes.Select(x => new CursorDefinition(x.Key, x.ParentNode.Key, x.ParentNode.Value, x.Location));
				result.AddRange(cursorDefinitions);
			}

			return result.ToLookup(n => n.Name, m => m);
		}

		public static ILookup<string, PaletteDefinition> GetPaletteDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods, HashSet<string> knownPaletteTypes)
		{
			var paletteDefinitions = new List<PaletteDefinition>();
			var yamlNodes = YamlNodesFromReferencedFiles(referencedFiles, mods);
			foreach (var node in yamlNodes)
			{
				if (node.ChildNodes == null)
					continue;

				foreach (var childNode in node.ChildNodes)
				{
					var split = childNode.Key.Split('@');
					var type = split[0];
					var identifier = split.Length > 1 ? split[1] : null;

					if (!knownPaletteTypes.Contains(type) || childNode.ChildNodes == null)
						continue;

					var name = childNode.ChildNodes.FirstOrDefault(y => y.Key == "Name")?.Value;
					var fileName = childNode.ChildNodes.FirstOrDefault(y => y.Key.ToLowerInvariant() == "filename")?.Value;
					var basePalette = childNode.ChildNodes.FirstOrDefault(y => y.Key.ToLowerInvariant() == "BasePalette".ToLowerInvariant())?.Value;
					var paletteLocation = new MemberLocation(childNode.Location.FileUri, childNode.Location.LineNumber, 1); // HACK HACK HACK: Until the YAML Loader learns about character positions, we hardcode 1 here (since this is all for traits on actor definitions).
					paletteDefinitions.Add(new PaletteDefinition(name, fileName, basePalette, identifier, type, paletteLocation));
				}
			}

			return paletteDefinitions.ToLookup(n => n.Name, m => m);
		}

		public static (IEnumerable<YamlNode> Original, IEnumerable<YamlNode> Flattened) ParseText(string text, string fileUriString = null)
		{
			var nodes = OpenRA.MiniYamlParser.MiniYamlLoader.FromString(text, discardCommentsAndWhitespace: false)
				.Select(x => x.ToYamlNode(fileUriString: fileUriString)).ToArray();

			var result = (nodes, nodes.SelectMany(FlattenChildNodes));
			if (!string.IsNullOrEmpty(fileUriString))
				foreach (var node in result.Item2)
					node.Location = new MemberLocation(node.Location.FileUri?.AbsoluteUri ?? fileUriString, node.Location.LineNumber, node.Location.CharacterPosition);

			return result;
		}

		static IEnumerable<YamlNode> FlattenChildNodes(YamlNode rootNode)
		{
			var nodes = new List<YamlNode> { rootNode };

			if (rootNode.ChildNodes == null)
				return nodes;

			foreach (var childNode in rootNode.ChildNodes)
				nodes.AddRange(FlattenChildNodes(childNode));

			return nodes;
		}

		static YamlNode ToYamlNode(this OpenRA.MiniYamlParser.MiniYamlNode source, YamlNode parentYamlNode = null, string fileUriString = null)
		{
			var result = new YamlNode
			{
				Key = source.Key,
				Value = source.Value.Value,
				Comment = source.Comment,
				Location = source.Location.ToMemberLocation(fileUriString),
				ParentNode = parentYamlNode
			};

			result.ChildNodes = source.Value.Nodes.Count > 0 ? source.Value.Nodes.Select(x => x.ToYamlNode(result)).ToList() : null;

			return result;
		}

		static MemberLocation ToMemberLocation(this OpenRA.MiniYamlParser.MiniYamlNode.SourceLocation source, string fileUriString = null)
		{
			return new MemberLocation(fileUriString ?? source.Filename, source.Line, source.Character);
		}

		static List<YamlNode> YamlNodesFromReferencedFiles(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods)
		{
			var result = new List<YamlNode>();

			var filePaths = referencedFiles.Select(fileReference => OpenRaFolderUtils.ResolveFilePath(fileReference, mods));
			foreach (var filePath in filePaths)
			{
				if (filePath == null)
					continue;

				var nodes = OpenRA.MiniYamlParser.MiniYamlLoader.FromFile(filePath.LocalPath);
				var yamlNodes = nodes.Select(x => x.ToYamlNode());
				result.AddRange(yamlNodes);
			}

			return result;
		}
	}
}

//  - List of actor definitions, each with its location, to be able to jump to a definition. (only from Inherits:)
//  - All files as arrays of parsed entities (actor/trait/property) to make lookups by line number fast af. (to easily find what the IDE/client is referencing on every request)
//
//  - Parse everything as YamlNodes?
//  Level1 nodes (top-level nodes) are either actor or weapon definitions. They can only reference other top-level nodes.
//  Level2 nodes (one-tab indentation) are traits for actors, but properties for weapons...
//	For actors they reference TraitInfos. Their values can reference top-level nodes.
//	For weapons they reference WeaponInfo properties. Their values can reference top-level nodes, an IProjectileInfo implementation or an IWarhead implementation.
//  Level3 nodes (two-tab indentation) are trait properties for actors,
//	For actors they are trait properties. Their values can reference top-level nodes, condition string literals or a name string literal defined by another Level3 node.
//	For weapons they are either ProjectileInfo or Warhead properties.
//
//
//  Use cases:
//	User opens OpenRA folder.	( ./OpenRA/ )
//	User opens mods folder.	( ./OpenRA/mods/ )
//	User opens a single mod's folder.	( ./OpenRA/mods/d2k/ )
//	User opens a subfolder of any mod.	( ./OpenRA/mods/d2k/rules/ )
