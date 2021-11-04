using System.Collections.Generic;

namespace OpenRA.MiniYamlParser
{
	// class? struct? record class? record struct?
	public class MiniYamlNode
	{
		public struct SourceLocation // This is fine as a nested struct, but it needs to move to MiniYaml.
		{
			public string Filename;
			public int Line;
			public int Character; // ?
			public override string ToString() { return $"{Filename}:{Line}"; }
		}

		public SourceLocation Location;
		public string Key;
		// public string Value;
		// public List<MiniYamlNode> ChildNodes;
		public MiniYamlNode ParentNode;
		public MiniYaml Value; // ew remove
		public string Comment;

		public MiniYamlNode(string k, MiniYaml v, string c = null)
		{
			Key = k;
			Value = v;
			Comment = c;
		}

		public MiniYamlNode(string k, MiniYaml v, string c, SourceLocation loc)
			: this(k, v, c)
		{
			Location = loc;
		}

		public MiniYamlNode(string k, string v, string c = null)
			: this(k, v, c, null) { }

		public MiniYamlNode(string k, string v, List<MiniYamlNode> n)
			: this(k, new MiniYaml(v, n), null) { }

		public MiniYamlNode(string k, string v, string c, List<MiniYamlNode> n)
			: this(k, new MiniYaml(v, n), c) { }

		public MiniYamlNode(string k, string v, string c, List<MiniYamlNode> n, SourceLocation loc)
			: this(k, new MiniYaml(v, n), c, loc) { }

		public override string ToString()
		{
			return $"{{YamlNode: {Key} @ {Location}}}";
		}

		public MiniYamlNode Clone()
		{
			return new MiniYamlNode(Key, Value.Clone());
		}
	}
}
