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
		public static IReadOnlyDictionary<string, ILookup<string, ActorDefinition>> GetActorDefinitions(in string modFolderPath)
		{
			var result = new List<YamlNode>();
			var actorDefinitionsPerMod = new Dictionary<string, List<ActorDefinition>>();

			// TODO: What about maps?
			var filePaths = Directory.EnumerateFiles(modFolderPath, "*.yaml", SearchOption.AllDirectories)
				.Where(x => x.Contains("/rules/") || x.Contains("\\rules\\") || ((x.Contains("/maps/") || x.Contains("\\maps\\")) && !x.EndsWith("map.yaml") && !x.EndsWith("weapons.yaml")));

			foreach (var filePath in filePaths)
			{
				var nodes = OpenRA.MiniYamlParser.MiniYamlLoader.FromFile(filePath);
				var yamlNodes = nodes.Select(x => x.ToYamlNode());
				result.AddRange(yamlNodes);
			}

			foreach (var node in result)
			{
				var location = new MemberLocation(node.Location.FilePath, node.Location.LineNumber, node.Location.CharacterPosition);

				var modId = OpenRaFolderUtils.GetModId(location.FilePath);
				if (!actorDefinitionsPerMod.ContainsKey(modId))
					actorDefinitionsPerMod.Add(modId, new List<ActorDefinition>());

				var actorTraits = node.ChildNodes == null
					? Enumerable.Empty<ActorTraitDefinition>()
					: node.ChildNodes.Select(x =>
						new ActorTraitDefinition(x.Key, new MemberLocation(x.Location.FilePath, x.Location.LineNumber, 1))); // HACK HACK HACK: Until the YAML Loader learns about character positions, we hardcode 1 here (since this is all for traits on actor definitions).

				actorDefinitionsPerMod[modId].Add(new ActorDefinition(node.Key, location, actorTraits.ToList()));
			}

			return actorDefinitionsPerMod.ToDictionary(x => x.Key, y => y.Value.ToLookup(n => n.Name, m => m));
		}

		public static IReadOnlyDictionary<string, ILookup<string, WeaponDefinition>> GetWeaponDefinitions(in string modFolderPath)
		{
			var result = new List<YamlNode>();
			var weaponDefinitionsPerMod = new Dictionary<string, List<WeaponDefinition>>();

			// TODO: What about maps?
			var filePaths = Directory.EnumerateFiles(modFolderPath, "*.yaml", SearchOption.AllDirectories)
				.Where(x => x.Contains("/weapons/") || x.Contains("\\weapons\\") || ((x.Contains("/maps/") || x.Contains("\\maps\\")) && x.EndsWith("weapons.yaml")));

			foreach (var filePath in filePaths)
			{
				var nodes = OpenRA.MiniYamlParser.MiniYamlLoader.FromFile(filePath);
				var yamlNodes = nodes.Select(x => x.ToYamlNode());
				result.AddRange(yamlNodes);
			}

			foreach (var node in result)
			{
				var location = new MemberLocation(node.Location.FilePath, node.Location.LineNumber, node.Location.CharacterPosition);

				var modId = OpenRaFolderUtils.GetModId(location.FilePath);
				if (!weaponDefinitionsPerMod.ContainsKey(modId))
					weaponDefinitionsPerMod.Add(modId, new List<WeaponDefinition>());

				var projectile = node.ChildNodes.FirstOrDefault(x => x.Key == "Projectile");
				var warheads = node.ChildNodes.Where(x => x.Key == "Warhead" || x.Key.StartsWith("Warhead@"));

				weaponDefinitionsPerMod[modId].Add(new WeaponDefinition(node.Key,
					projectile == null ? default : new WeaponProjectileDefinition(projectile.Value, new MemberLocation(projectile.Location.FilePath, projectile.Location.LineNumber, projectile.Location.CharacterPosition)),
					warheads.Select(x => new WeaponWarheadDefinition(x.Value, new MemberLocation(x.Location.FilePath, x.Location.LineNumber, x.Location.CharacterPosition))).ToArray(),
					location));
			}

			return weaponDefinitionsPerMod.ToDictionary(x => x.Key, y => y.Value.ToLookup(n => n.Name, m => m));
		}

		public static IReadOnlyDictionary<string, ILookup<string, ConditionDefinition>> GetConditionDefinitions(in string modFolderPath)
		{
			var result = new List<ConditionDefinition>();

			// TODO: What about maps?
			var filePaths = Directory.EnumerateFiles(modFolderPath, "*.yaml", SearchOption.AllDirectories)
				.Where(x => x.Contains("/rules/") || x.Contains("\\rules\\"));

			foreach (var filePath in filePaths)
			{
				var nodes = OpenRA.MiniYamlParser.MiniYamlLoader.FromFile(filePath);
				var yamlNodes = nodes.Select(x => x.ToYamlNode());
				var flattenedNodes = yamlNodes.SelectMany(FlattenChildNodes);
				var conditions = flattenedNodes
					.Where(x => x.Key.EndsWith("Condition") && !string.IsNullOrWhiteSpace(x.Value))
					.Select(x => new ConditionDefinition(x.Value.TrimStart('!'), x.Location));

				result.AddRange(conditions);
			}

			return result.GroupBy(x => OpenRaFolderUtils.GetModId(x.Location.FilePath))
				.ToDictionary(x => x.Key,
					y => y.ToLookup(n => n.Name, m => m));
		}

		public static IEnumerable<YamlNode> ParseText(string text, bool flatten = false)
		{
			var nodes = OpenRA.MiniYamlParser.MiniYamlLoader.FromString(text, discardCommentsAndWhitespace: false).Select(x => x.ToYamlNode());
			return flatten ? nodes.SelectMany(FlattenChildNodes) : nodes;
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
