using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Oraide.Core.Entities;
using Oraide.Core.Entities.MiniYaml;

namespace Oraide.MiniYaml.YamlParsers
{
	public static class OpenRAMiniYamlParser
	{
		public static IReadOnlyDictionary<string, ReadOnlyCollection<YamlNode>> GetParsedRulesPerFile(in string modFolderPath)
		{
			var result = new Dictionary<string, ReadOnlyCollection<YamlNode>>();
			var filePaths = Directory.EnumerateFiles(modFolderPath, "*.yaml", SearchOption.AllDirectories);
			foreach (var filePath in filePaths)
			{
				var nodes = OpenRA.MiniYamlParser.MiniYamlLoader.FromFile(filePath, false);
				var yamlNodes = nodes.Select(x => x.ToYamlNode());
				var flattenedNodes = yamlNodes.SelectMany(FlattenChildNodes);
				result[filePath] = new ReadOnlyCollection<YamlNode>(flattenedNodes.ToList());
			}

			return result;
		}

		public static ILookup<string, ActorDefinition> GetActorDefinitions(in string modFolderPath)
		{
			var result = new List<YamlNode>();
			var actorDefinitions = new List<ActorDefinition>();

			var filePaths = Directory.EnumerateFiles(modFolderPath, "*.yaml", SearchOption.AllDirectories);
			foreach (var filePath in filePaths)
			{
				var nodes = OpenRA.MiniYamlParser.MiniYamlLoader.FromFile(filePath);
				var yamlNodes = nodes.Select(x => x.ToYamlNode());
				result.AddRange(yamlNodes);
			}

			foreach (var node in result)
			{
				var location = new MemberLocation(node.Location.FilePath, node.Location.LineNumber, node.Location.CharacterPosition);
				actorDefinitions.Add(new ActorDefinition(node.Key, location, new List<TraitDefinition>()));
			}

			return actorDefinitions.ToLookup(x => x.Name, y => y);
		}

		public static ILookup<string, MemberLocation> GetConditionDefinitions(in string modFolderPath)
		{
			var result = new List<KeyValuePair<string, MemberLocation>>();

			var filePaths = Directory.EnumerateFiles(modFolderPath, "*.yaml", SearchOption.AllDirectories);
			foreach (var filePath in filePaths)
			{
				var nodes = OpenRA.MiniYamlParser.MiniYamlLoader.FromFile(filePath);
				var yamlNodes = nodes.Select(x => x.ToYamlNode());
				var flattenedNodes = yamlNodes.SelectMany(FlattenChildNodes);
				var conditions = flattenedNodes
					.Where(x => x.Key.EndsWith("Condition") && !string.IsNullOrWhiteSpace(x.Value))
					.Select(x => new KeyValuePair<string, MemberLocation>(x.Value.TrimStart('!'), x.Location));

				result.AddRange(conditions);
			}

			return result.ToLookup(x => x.Key, y => y.Value);
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

		static YamlNode ToYamlNode(this OpenRA.MiniYamlParser.MiniYamlNode source, YamlNode parentYamlNode = null)
		{
			var result = new YamlNode
			{
				Key = source.Key,
				Value = source.Value.Value,
				Comment = source.Comment,
				Location = source.Location.ToMemberLocation(),
				ParentNode = parentYamlNode
			};

			result.ChildNodes = source.Value.Nodes.Count > 0 ? source.Value.Nodes.Select(x => x.ToYamlNode(result)).ToList() : null;

			return result;
		}

		static MemberLocation ToMemberLocation(this OpenRA.MiniYamlParser.MiniYamlNode.SourceLocation source)
		{
			return new MemberLocation(source.Filename, source.Line, source.Character);
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
