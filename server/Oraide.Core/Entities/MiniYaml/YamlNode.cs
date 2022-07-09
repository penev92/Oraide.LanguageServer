using System.Collections.Generic;

namespace Oraide.Core.Entities.MiniYaml
{
	public class YamlNode
	{
		public string Key;
		public string Value;
		public string Comment;
		public MemberLocation Location;
		public YamlNode ParentNode;
		public List<YamlNode> ChildNodes;

		public override string ToString() => $"{nameof(YamlNode)}: `{Key}: {Value}` @ {Location}";
	}
}
