using System.Collections.Generic;
using System.IO;
using System.Linq;
using Oraide.Core;
using Oraide.Core.Entities;
using Oraide.Core.Entities.MiniYaml;

namespace Oraide.MiniYaml.Abstraction.Parsers
{
	public abstract class BaseMiniYamlParser : IMiniYamlParser
	{
		public abstract bool CanParse(in string folderPath);

		public virtual IEnumerable<YamlNode> ParseModFile(in string modFolder)
		{
			return OpenRA.MiniYamlParser.MiniYamlLoader.FromFile(Path.Combine(modFolder, "mod.yaml")).Select(x => x.ToYamlNode());
		}

		public virtual IEnumerable<YamlNode> ParseMapFile(in string mapFolder)
		{
			return OpenRA.MiniYamlParser.MiniYamlLoader.FromFile(Path.Combine(mapFolder, "map.yaml")).Select(x => x.ToYamlNode());
		}

		public virtual IEnumerable<ActorDefinition> ParseActorDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods)
		{
			var yamlNodes = YamlNodesFromReferencedFiles(referencedFiles, mods);
			foreach (var node in yamlNodes)
			{
				var location = new MemberLocation(node.Location.FileUri, node.Location.LineNumber, node.Location.CharacterPosition);

				var actorTraits = node.ChildNodes == null
					? Enumerable.Empty<ActorTraitDefinition>()
					: node.ChildNodes.Select(x =>
						new ActorTraitDefinition(x.Key, new MemberLocation(x.Location.FileUri, x.Location.LineNumber, 1))); // HACK HACK HACK: Until the YAML Loader learns about character positions, we hardcode 1 here (since this is all for traits on actor definitions).

				var tooltipTrait = node.ChildNodes?.FirstOrDefault(x => x.Key.EndsWith("Tooltip"));
				var displayName = tooltipTrait?.ChildNodes?.FirstOrDefault(x => x.Key == "Name")?.Value;

				yield return new ActorDefinition(node.Key, displayName, location, actorTraits.ToList());
			}
		}

		public virtual IEnumerable<WeaponDefinition> ParseWeaponDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods)
		{
			var yamlNodes = YamlNodesFromReferencedFiles(referencedFiles, mods);
			foreach (var node in yamlNodes)
			{
				var location = new MemberLocation(node.Location.FileUri, node.Location.LineNumber, node.Location.CharacterPosition);

				var projectile = node.ChildNodes?.FirstOrDefault(x => x.Key == "Projectile");
				var warheads = node.ChildNodes?.Where(x => x.Key == "Warhead" || x.Key.StartsWith("Warhead@")) ?? Enumerable.Empty<YamlNode>();

				yield return new WeaponDefinition(node.Key,
					projectile == null ? default : new WeaponProjectileDefinition(projectile.Value, new MemberLocation(projectile.Location.FileUri, projectile.Location.LineNumber, projectile.Location.CharacterPosition)),
					warheads.Select(x => new WeaponWarheadDefinition(x.Value, new MemberLocation(x.Location.FileUri, x.Location.LineNumber, x.Location.CharacterPosition))).ToArray(),
					location);
			}
		}

		public virtual IEnumerable<ConditionDefinition> ParseConditionDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods)
		{
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

				foreach (var conditionDefinition in conditions)
					yield return conditionDefinition;
			}
		}

		public virtual IEnumerable<CursorDefinition> ParseCursorDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods)
		{
			var fileUris = referencedFiles.Select(fileReference => OpenRaFolderUtils.ResolveFilePath(fileReference, mods));
			foreach (var fileUri in fileUris)
			{
				if (fileUri == null)
					continue;

				var nodes = OpenRA.MiniYamlParser.MiniYamlLoader.FromFile(fileUri.LocalPath);
				var yamlNodes = nodes.SelectMany(x =>
					x.Value.Nodes.SelectMany(y =>
						y.Value.Nodes.Select(z => z.ToYamlNode(y.ToYamlNode()))));

				foreach (var yamlNode in yamlNodes)
					yield return new CursorDefinition(yamlNode.Key, yamlNode.ParentNode.Key, yamlNode.ParentNode.Value, yamlNode.Location);
			}
		}

		public virtual IEnumerable<PaletteDefinition> ParsePaletteDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods, HashSet<string> knownPaletteTypes)
		{
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

					yield return new PaletteDefinition(name, fileName, basePalette, identifier, type, paletteLocation);
				}
			}
		}

		public virtual IEnumerable<SpriteSequenceImageDefinition> ParseSpriteSequenceDefinitions(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods)
		{
			var yamlNodes = YamlNodesFromReferencedFiles(referencedFiles, mods);
			foreach (var node in yamlNodes)
			{
				var location = new MemberLocation(node.Location.FileUri, node.Location.LineNumber, node.Location.CharacterPosition);

				var sequences = node.ChildNodes == null
					? Enumerable.Empty<SpriteSequenceDefinition>()
					: node.ChildNodes.Select(x =>
						new SpriteSequenceDefinition(x.Key, x.ParentNode.Key, x.Value, new MemberLocation(x.Location.FileUri, x.Location.LineNumber, 1))); // HACK HACK HACK: Until the YAML Loader learns about character positions, we hardcode 1 here (since this is all for traits on actor definitions).

				yield return new SpriteSequenceImageDefinition(node.Key, location, sequences.ToArray());
			}
		}

		public virtual (IEnumerable<YamlNode> Original, IEnumerable<YamlNode> Flattened) ParseText(string text, string fileUriString = null)
		{
			var nodes = OpenRA.MiniYamlParser.MiniYamlLoader.FromString(text, discardCommentsAndWhitespace: false)
				.Select(x => x.ToYamlNode(fileUriString: fileUriString)).ToArray();

			var result = (nodes, nodes.SelectMany(FlattenChildNodes));
			if (!string.IsNullOrEmpty(fileUriString))
				foreach (var node in result.Item2)
					node.Location = new MemberLocation(node.Location.FileUri?.AbsoluteUri ?? fileUriString, node.Location.LineNumber, node.Location.CharacterPosition);

			return result;
		}

		#region Protected methods

		protected List<YamlNode> YamlNodesFromReferencedFiles(IEnumerable<string> referencedFiles, IReadOnlyDictionary<string, string> mods)
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

		protected IEnumerable<YamlNode> FlattenChildNodes(YamlNode rootNode)
		{
			var nodes = new List<YamlNode> { rootNode };

			if (rootNode.ChildNodes == null)
				return nodes;

			foreach (var childNode in rootNode.ChildNodes)
				nodes.AddRange(FlattenChildNodes(childNode));

			return nodes;
		}

		#endregion
	}
}
