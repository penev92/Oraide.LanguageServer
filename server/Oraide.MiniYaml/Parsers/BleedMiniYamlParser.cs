using Oraide.MiniYaml.Abstraction.Parsers;

namespace Oraide.MiniYaml.Parsers
{
	public class BleedMiniYamlParser : BaseMiniYamlParser
	{
		public override bool CanParse(in string folderPath)
		{
			return true;
		}
	}
}
