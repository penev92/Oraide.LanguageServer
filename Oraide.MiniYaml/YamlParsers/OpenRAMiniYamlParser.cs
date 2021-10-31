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
		public static Dictionary<string, ActorDefinition> ParseRulesOld(in string modFolderPath)
		{
			var allStuff = new List<OpenRA.MiniYamlParser.MiniYamlNode>();
			var currentActorTraits = new List<TraitDefinition>();
			var actorDefinitions = new Dictionary<string, ActorDefinition>();

			var filePaths = Directory.EnumerateFiles(modFolderPath, "*.yaml", SearchOption.AllDirectories);
			foreach (var filePath in filePaths)
			{
				var nodes = OpenRA.MiniYamlParser.MiniYamlLoader.FromFile(filePath);
				allStuff.AddRange(nodes);
			}

			// var player = allStuff.Where(x => x.Key == "Player");

			return actorDefinitions;
		}

		public static IReadOnlyDictionary<string, ReadOnlyCollection<OpenRA.MiniYamlParser.MiniYamlNode>> GetParsedRulesPerFile(in string modFolderPath)
		{
			var yamlNodes = new Dictionary<string, ReadOnlyCollection<OpenRA.MiniYamlParser.MiniYamlNode>>();
			var filePaths = Directory.EnumerateFiles(modFolderPath, "*.yaml", SearchOption.AllDirectories);
			foreach (var filePath in filePaths)
			{
				var nodes = OpenRA.MiniYamlParser.MiniYamlLoader.FromFile(filePath, false);
				var flattenedNodes = nodes.SelectMany(FlattenChildNodes);
				yamlNodes[filePath] = new ReadOnlyCollection<OpenRA.MiniYamlParser.MiniYamlNode>(flattenedNodes.ToList());
			}

			return yamlNodes;
		}

		public static ILookup<string, ActorDefinition> GetActorDefinitions(in string modFolderPath)
		{
			var yamlNodes = new List<OpenRA.MiniYamlParser.MiniYamlNode>();
			var actorDefinitions = new List<ActorDefinition>();

			var filePaths = Directory.EnumerateFiles(modFolderPath, "*.yaml", SearchOption.AllDirectories);
			foreach (var filePath in filePaths)
			{
				var nodes = OpenRA.MiniYamlParser.MiniYamlLoader.FromFile(filePath);
				yamlNodes.AddRange(nodes);
			}

			foreach (var node in yamlNodes)
			{
				var location = new MemberLocation(node.Location.Filename, node.Location.Line, 0);
				actorDefinitions.Add(new ActorDefinition(node.Key, location, new List<TraitDefinition>()));
			}

			return actorDefinitions.ToLookup(x => x.Name, y => y);
		}

		static IEnumerable<OpenRA.MiniYamlParser.MiniYamlNode> FlattenChildNodes(OpenRA.MiniYamlParser.MiniYamlNode rootNode)
		{
			var nodes = new List<OpenRA.MiniYamlParser.MiniYamlNode> { rootNode };

			foreach (var childNode in rootNode.Value.Nodes)
				nodes.AddRange(FlattenChildNodes(childNode));

			return nodes;
		}
	}
}
