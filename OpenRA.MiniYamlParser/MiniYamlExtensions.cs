using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenRA.MiniYamlParser
{
	public static class MiniYamlExtensions
	{
		public static void WriteToFile(this List<MiniYamlNode> y, string filename)
		{
			File.WriteAllLines(filename, y.ToLines().Select(x => x.TrimEnd()).ToArray());
		}

		public static string WriteToString(this List<MiniYamlNode> y)
		{
			// Remove all trailing newlines and restore the final EOF newline
			return y.ToLines().JoinWith("\n").TrimEnd('\n') + "\n";
		}

		public static IEnumerable<string> ToLines(this List<MiniYamlNode> y)
		{
			foreach (var kv in y)
				foreach (var line in kv.Value.ToLines(kv.Key, kv.Comment))
					yield return line;
		}
	}
}
