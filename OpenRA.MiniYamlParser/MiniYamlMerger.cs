using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.MiniYamlParser
{
	public static class MiniYamlMerger
	{
		public static List<MiniYamlNode> Merge(IEnumerable<List<MiniYamlNode>> sources)
		{
			if (!sources.Any())
				return new List<MiniYamlNode>();

			var tree = sources.Where(s => s != null)
				.Select(MergeSelfPartial)
				.Aggregate(MergePartial)
				.Where(n => n.Key != null)
				.ToDictionary(n => n.Key, n => n.Value);

			var resolved = new Dictionary<string, MiniYaml>();
			foreach (var kv in tree)
			{
				var inherited = new Dictionary<string, MiniYamlNode.SourceLocation>();
				inherited.Add(kv.Key, default(MiniYamlNode.SourceLocation));

				var children = ResolveInherits(kv.Key, kv.Value, tree, inherited);
				resolved.Add(kv.Key, new MiniYaml(kv.Value.Value, children));
			}

			// Resolve any top-level removals (e.g. removing whole actor blocks)
			var nodes = new MiniYaml("", resolved.Select(kv => new MiniYamlNode(kv.Key, kv.Value)).ToList());
			return ResolveInherits("", nodes, tree, new Dictionary<string, MiniYamlNode.SourceLocation>());
		}

		static void MergeIntoResolved(MiniYamlNode overrideNode, List<MiniYamlNode> existingNodes,
			Dictionary<string, MiniYaml> tree, Dictionary<string, MiniYamlNode.SourceLocation> inherited)
		{
			var existingNode = existingNodes.FirstOrDefault(n => n.Key == overrideNode.Key);
			if (existingNode != null)
			{
				existingNode.Value = MergePartial(existingNode.Value, overrideNode.Value);
				existingNode.Value.Nodes = ResolveInherits(existingNode.Key, existingNode.Value, tree, inherited);
			}
			else
				existingNodes.Add(overrideNode.Clone());
		}

		/// <summary>
		/// Merges any duplicate keys that are defined within the same set of nodes.
		/// Does not resolve inheritance or node removals.
		/// </summary>
		static MiniYaml MergeSelfPartial(MiniYaml existingNodes)
		{
			// Nothing to do
			if (existingNodes.Nodes == null || existingNodes.Nodes.Count == 0)
				return existingNodes;

			return new MiniYaml(existingNodes.Value, MergeSelfPartial(existingNodes.Nodes));
		}

		/// <summary>
		/// Merges any duplicate keys that are defined within the same set of nodes.
		/// Does not resolve inheritance or node removals.
		/// </summary>
		static List<MiniYamlNode> MergeSelfPartial(List<MiniYamlNode> existingNodes)
		{
			var keys = new HashSet<string>();
			var ret = new List<MiniYamlNode>();
			foreach (var n in existingNodes)
			{
				if (keys.Add(n.Key))
					ret.Add(n);
				else
				{
					// Node with the same key has already been added: merge new node over the existing one
					var original = ret.First(r => r.Key == n.Key);
					original.Value = MergePartial(original.Value, n.Value);
				}
			}

			ret.TrimExcess();
			return ret;
		}

		static MiniYaml MergePartial(MiniYaml existingNodes, MiniYaml overrideNodes)
		{
			if (existingNodes == null)
				return overrideNodes;

			if (overrideNodes == null)
				return existingNodes;

			return new MiniYaml(overrideNodes.Value ?? existingNodes.Value, MergePartial(existingNodes.Nodes, overrideNodes.Nodes));
		}

		static List<MiniYamlNode> MergePartial(List<MiniYamlNode> existingNodes, List<MiniYamlNode> overrideNodes)
		{
			if (existingNodes.Count == 0)
				return overrideNodes;

			if (overrideNodes.Count == 0)
				return existingNodes;

			var ret = new List<MiniYamlNode>();

			var existingDict = existingNodes.ToDictionaryWithConflictLog(x => x.Key, "MiniYaml.Merge", null, x => $"{x.Key} (at {x.Location})");
			var overrideDict = overrideNodes.ToDictionaryWithConflictLog(x => x.Key, "MiniYaml.Merge", null, x => $"{x.Key} (at {x.Location})");
			var allKeys = existingDict.Keys.Union(overrideDict.Keys);

			foreach (var key in allKeys)
			{
				existingDict.TryGetValue(key, out var existingNode);
				overrideDict.TryGetValue(key, out var overrideNode);

				var loc = overrideNode?.Location ?? default;
				var comment = (overrideNode ?? existingNode).Comment;
				var merged = (existingNode == null || overrideNode == null) ? overrideNode ?? existingNode :
					new MiniYamlNode(key, MergePartial(existingNode.Value, overrideNode.Value), comment, loc);
				ret.Add(merged);
			}

			ret.TrimExcess();
			return ret;
		}

		static List<MiniYamlNode> ResolveInherits(string key, MiniYaml node, Dictionary<string, MiniYaml> tree, Dictionary<string, MiniYamlNode.SourceLocation> inherited)
		{
			var resolved = new List<MiniYamlNode>(node.Nodes.Count);

			// Inheritance is tracked from parent->child, but not from child->parentsiblings.
			inherited = new Dictionary<string, MiniYamlNode.SourceLocation>(inherited);

			foreach (var n in node.Nodes)
			{
				if (n.Key == "Inherits" || n.Key.StartsWith("Inherits@", StringComparison.Ordinal))
				{
					if (!tree.TryGetValue(n.Value.Value, out var parent))
						throw new YamlException(
							$"{n.Location}: Parent type `{n.Value.Value}` not found");

					if (inherited.ContainsKey(n.Value.Value))
						throw new YamlException($"{n.Location}: Parent type `{n.Value.Value}` was already inherited by this yaml tree at {inherited[n.Value.Value]} (note: may be from a derived tree)");

					inherited.Add(n.Value.Value, n.Location);
					foreach (var r in ResolveInherits(n.Key, parent, tree, inherited))
						MergeIntoResolved(r, resolved, tree, inherited);
				}
				else if (n.Key.StartsWith("-", StringComparison.Ordinal))
				{
					var removed = n.Key.Substring(1);
					if (resolved.RemoveAll(r => r.Key == removed) == 0)
						throw new YamlException($"{n.Location}: There are no elements with key `{removed}` to remove");
				}
				else
					MergeIntoResolved(n, resolved, tree, inherited);
			}

			resolved.TrimExcess();
			return resolved;
		}
	}
}
