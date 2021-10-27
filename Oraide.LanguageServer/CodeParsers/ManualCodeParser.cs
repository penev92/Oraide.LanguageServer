using System;
using System.Collections.Generic;
using System.IO;

namespace Oraide.LanguageServer.CodeParsers
{
	public static class ManualCodeParser
	{
		public static IDictionary<string, TraitInfo> Parse(in string oraFolderPath)
		{
			var traitDictionary = new Dictionary<string, TraitInfo>();
			
			var filePaths = Directory.EnumerateFiles(oraFolderPath, "*.cs", SearchOption.AllDirectories);
			foreach (var filePath in filePaths/*.Where(x => x.Contains("Disguise"))*/)
			{
				var text = File.ReadAllText(filePath);
				var fileName = Path.GetFileName(filePath).Split('.')[0];
				var subTextStart = text.IndexOf($"class {fileName}Info", StringComparison.Ordinal);

				if (filePath.Contains("Trait") && subTextStart > 0)
				{
					// Parse trait properties.
					var traitProperties = new List<TraitPropertyInfo>();
					var subText = text.Substring(subTextStart + 5);
					subText = subText.Substring(0, subText.IndexOf("class "));
			
					var lines = subText.Split("\n");
					foreach (var line in lines)
					{
						if (line.Contains("public readonly"))
						{
							var cleanLine = line.Replace(", ", ",_"); // HACK: Dealing with Dictionary<T, Y>.
							var propName = cleanLine.Split(' ')[3];
							var charPosition = cleanLine.IndexOf(propName, StringComparison.Ordinal);
							traitProperties.Add(new TraitPropertyInfo(propName, new MemberLocation(filePath, 0, charPosition)));
						}
					}
			
					// Finally, add the TraitInfo to the list of loaded TraitInfos.
					var location = new MemberLocation(filePath, 0, 0); // TODO:
					var traitInfo = new TraitInfo(fileName, "", location, Array.Empty<string>(), traitProperties.ToArray());
					traitDictionary.Add(fileName, traitInfo);
				}
			}

			return traitDictionary;
		}
	}
}
