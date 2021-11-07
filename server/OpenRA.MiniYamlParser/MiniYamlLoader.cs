using System;
using System.Collections.Generic;
using System.IO;

namespace OpenRA.MiniYamlParser
{
	public static class MiniYamlLoader
	{
		public static List<MiniYamlNode> FromFile(string path, bool discardCommentsAndWhitespace = true, Dictionary<string, string> stringPool = null)
		{
			return FromLines(File.ReadAllLines(path), path, discardCommentsAndWhitespace, stringPool);
		}

		public static List<MiniYamlNode> FromStream(Stream s, string fileName = "<no filename available>", bool discardCommentsAndWhitespace = true, Dictionary<string, string> stringPool = null)
		{
			IEnumerable<string> Lines(StreamReader reader)
			{
				string line;
				while ((line = reader.ReadLine()) != null)
					yield return line;
			}

			using (var reader = new StreamReader(s))
				return FromLines(Lines(reader), fileName, discardCommentsAndWhitespace, stringPool);
		}

		public static List<MiniYamlNode> FromString(string text, string fileName = "<no filename available>", bool discardCommentsAndWhitespace = true, Dictionary<string, string> stringPool = null)
		{
			return FromLines(text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None), fileName, discardCommentsAndWhitespace, stringPool);
		}

		static List<MiniYamlNode> FromLines(IEnumerable<string> lines, string filename, bool discardCommentsAndWhitespace, Dictionary<string, string> stringPool)
		{
			if (stringPool == null)
				stringPool = new Dictionary<string, string>();

			var levels = new List<List<MiniYamlNode>>();
			levels.Add(new List<MiniYamlNode>());

			var lineNo = 0;
			foreach (var ll in lines)
			{
				var line = ll;
				++lineNo;

				var keyStart = 0;
				var level = 0;
				var spaces = 0;
				var textStart = false;

				string key = null;
				string value = null;
				string comment = null;
				var location = new MiniYamlNode.SourceLocation { Filename = filename, Line = lineNo };

				if (line.Length > 0)
				{
					var currChar = line[keyStart];

					while (!(currChar == '\n' || currChar == '\r') && keyStart < line.Length && !textStart)
					{
						currChar = line[keyStart];
						switch (currChar)
						{
							case ' ':
								spaces++;
								if (spaces >= MiniYaml.SpacesPerLevel)
								{
									spaces = 0;
									level++;
								}

								keyStart++;
								break;
							case '\t':
								level++;
								keyStart++;
								break;
							default:
								textStart = true;
								break;
						}
					}

					if (levels.Count <= level)
						throw new YamlException($"Bad indent in miniyaml at {location}");

					while (levels.Count > level + 1)
						levels.RemoveAt(levels.Count - 1);

					// Extract key, value, comment from line as `<key>: <value>#<comment>`
					// The # character is allowed in the value if escaped (\#).
					// Leading and trailing whitespace is always trimmed from keys.
					// Leading and trailing whitespace is trimmed from values unless they
					// are marked with leading or trailing backslashes
					var keyLength = line.Length - keyStart;
					var valueStart = -1;
					var valueLength = 0;
					var commentStart = -1;
					for (var i = 0; i < line.Length; i++)
					{
						if (valueStart < 0 && line[i] == ':')
						{
							valueStart = i + 1;
							keyLength = i - keyStart;
							valueLength = line.Length - i - 1;
						}

						if (commentStart < 0 && line[i] == '#' && (i == 0 || line[i - 1] != '\\'))
						{
							commentStart = i + 1;
							if (commentStart <= keyLength)
								keyLength = i - keyStart;
							else
								valueLength = i - valueStart;

							break;
						}
					}

					if (keyLength > 0)
						key = line.Substring(keyStart, keyLength).Trim();

					if (valueStart >= 0)
					{
						var trimmed = line.Substring(valueStart, valueLength).Trim();
						if (trimmed.Length > 0)
							value = trimmed;
					}

					if (commentStart >= 0 && !discardCommentsAndWhitespace)
						comment = line.Substring(commentStart);

					// Remove leading/trailing whitespace guards
					if (value != null && value.Length > 1)
					{
						var trimLeading = value[0] == '\\' && (value[1] == ' ' || value[1] == '\t') ? 1 : 0;
						var trimTrailing = value[value.Length - 1] == '\\' && (value[value.Length - 2] == ' ' || value[value.Length - 2] == '\t') ? 1 : 0;
						if (trimLeading + trimTrailing > 0)
							value = value.Substring(trimLeading, value.Length - trimLeading - trimTrailing);
					}

					// Remove escape characters from #
					if (value != null && value.IndexOf('#') != -1)
						value = value.Replace("\\#", "#");
				}

				if (key != null || !discardCommentsAndWhitespace)
				{
					key = key == null ? null : stringPool.GetOrAdd(key, key);
					value = value == null ? null : stringPool.GetOrAdd(value, value);
					comment = comment == null ? null : stringPool.GetOrAdd(comment, comment);

					var nodes = new List<MiniYamlNode>();
					levels[level].Add(new MiniYamlNode(key, value, comment, nodes, location));

					levels.Add(nodes);
				}
			}

			foreach (var nodes in levels)
				nodes.TrimExcess();

			return levels[0];
		}
	}
}
