using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Oraide.Core.Entities.MiniYaml;
using Oraide.MiniYaml;

namespace Oraide.LanguageServer.Caching
{
	public class OpenFileCache
	{
		private readonly YamlInformationProvider yamlInformationProvider;

		private readonly IDictionary<string, (ReadOnlyCollection<string>, ReadOnlyCollection<YamlNode>)> openFiles =
			new Dictionary<string, (ReadOnlyCollection<string> Lines, ReadOnlyCollection<YamlNode> YamlNodes)>();

		public OpenFileCache()
		{
			// TODO: Get this via DI:
			yamlInformationProvider = new YamlInformationProvider(null);
		}

		public (ReadOnlyCollection<string>, ReadOnlyCollection<YamlNode>) this[string filePath] => openFiles[filePath];

		public void AddOrUpdateOpenFile(string filePath)
		{
			var text = File.ReadAllText(filePath);
			AddOrUpdateOpenFile(filePath, text);
		}

		public void AddOrUpdateOpenFile(string filePath, string content)
		{
			var lines = content.Split(new[] {"\r\n", "\n"}, StringSplitOptions.None);
			var nodes = yamlInformationProvider.ParseText(content, true);
			openFiles[filePath] = (new ReadOnlyCollection<string>(lines), new ReadOnlyCollection<YamlNode>(nodes.ToArray()));
		}

		public void RemoveOpenFile(string filePath)
		{
			openFiles.Remove(filePath);
		}

		public bool ContainsFile(string filePath)
		{
			return openFiles.ContainsKey(filePath);
		}
	}
}
