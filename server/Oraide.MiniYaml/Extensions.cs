using System.Linq;
using Oraide.Core.Entities;
using Oraide.Core.Entities.MiniYaml;

namespace Oraide.MiniYaml
{
	public static class MiniYamlExtensions
	{
		internal static YamlNode ToYamlNode(this OpenRA.MiniYamlParser.MiniYamlNode source, YamlNode parentYamlNode = null, string fileUriString = null)
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
	}
}
