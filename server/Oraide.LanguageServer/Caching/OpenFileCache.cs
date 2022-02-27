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

		public void AddOrUpdateOpenFile(string fileUri)
		{
			var filePath = fileUri.Replace("file:///", string.Empty).Replace("%3A", ":");
			var text = File.ReadAllText(filePath);
			AddOrUpdateOpenFile(fileUri, text);
		}

		public void AddOrUpdateOpenFile(string fileUri, string content)
		{
			var lines = content.Split(new[] {"\r\n", "\n"}, StringSplitOptions.None);
			var nodes = yamlInformationProvider.ParseText(content, true);
			openFiles[fileUri] = (new ReadOnlyCollection<string>(lines), new ReadOnlyCollection<YamlNode>(nodes.ToArray()));
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
