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

		private readonly IDictionary<string, (ReadOnlyCollection<YamlNode> YamlNodes, ReadOnlyCollection<YamlNode> FlattenedYamlNodes, ReadOnlyCollection<string> Lines)>
			openFiles = new Dictionary<string, (ReadOnlyCollection<YamlNode> YamlNodes, ReadOnlyCollection<YamlNode> FlattenedYamlNodes, ReadOnlyCollection<string> Lines)>();

		public OpenFileCache()
		{
			// TODO: Get this via DI:
			yamlInformationProvider = new YamlInformationProvider(null);
		}

		public (ReadOnlyCollection<YamlNode> YamlNodes,
			ReadOnlyCollection<YamlNode> FlattenedYamlNodes,
			ReadOnlyCollection<string> Lines)
			this[string filePath] => openFiles[filePath];

		public void AddOrUpdateOpenFile(string fileUri)
		{
			var filePath = fileUri.Replace("file:///", string.Empty).Replace("%3A", ":");
			var text = File.ReadAllText(filePath);
			AddOrUpdateOpenFile(fileUri, text);
		}

		public void AddOrUpdateOpenFile(string fileUri, string content)
		{
			var lines = content.Split(new[] {"\r\n", "\n"}, StringSplitOptions.None);
			var nodes = yamlInformationProvider.ParseText(content);
			openFiles[fileUri] = (
				new ReadOnlyCollection<YamlNode>(nodes.Original.ToArray()),
				new ReadOnlyCollection<YamlNode>(nodes.Flattened.ToArray()),
				new ReadOnlyCollection<string>(lines));
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
