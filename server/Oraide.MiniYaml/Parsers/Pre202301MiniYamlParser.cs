using System.IO;
using System.Linq;
using Oraide.MiniYaml.Abstraction.Parsers;

namespace Oraide.MiniYaml.Parsers
{
	/// <summary>
	/// This was bleed before https://github.com/OpenRA/OpenRA/pull/20580 changed how file names are defined on sprite sequences.
	/// </summary>
	public class Pre202301MiniYamlParser : BaseMiniYamlParser
	{
		public override bool CanParse(in string folderPath)
		{
			return Directory.EnumerateFiles(folderPath, "*.yaml", SearchOption.AllDirectories).Any(x => File.ReadAllText(x).Contains("\n\t\tAddExtension: False"));
		}
	}
}
