using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Oraide.Core;
using Oraide.Core.Entities.MiniYaml;
using Oraide.MiniYaml;

namespace Oraide.LanguageServer.Caching
{
	public class OpenFileCache
	{
		readonly YamlInformationProvider yamlInformationProvider;

		readonly IDictionary<string, (ReadOnlyCollection<YamlNode> YamlNodes, ReadOnlyCollection<YamlNode> FlattenedYamlNodes, ReadOnlyCollection<string> Lines)>
			openFiles = new Dictionary<string, (ReadOnlyCollection<YamlNode> YamlNodes, ReadOnlyCollection<YamlNode> FlattenedYamlNodes, ReadOnlyCollection<string> Lines)>();

		public OpenFileCache(YamlInformationProvider yamlInformationProvider)
		{
			this.yamlInformationProvider = yamlInformationProvider;
		}

		public (ReadOnlyCollection<YamlNode> YamlNodes,
			ReadOnlyCollection<YamlNode> FlattenedYamlNodes,
			ReadOnlyCollection<string> Lines)
			this[string filePath] => openFiles[filePath];

		public void AddOrUpdateOpenFile(string fileUriString)
		{
			var uriString = OpenRaFolderUtils.NormalizeFileUriString(fileUriString);
			var filePath = new Uri(uriString).AbsolutePath;
			var text = File.ReadAllText(filePath);
			AddOrUpdateOpenFile(uriString, text);
		}

		public void AddOrUpdateOpenFile(string fileUriString, string content)
		{
			var uriString = OpenRaFolderUtils.NormalizeFileUriString(fileUriString);
			var lines = content.Split(new[] {"\r\n", "\n"}, StringSplitOptions.None);
			var nodes = yamlInformationProvider.ParseText(content, uriString);
			openFiles[uriString] = (
				new ReadOnlyCollection<YamlNode>(nodes.Original.ToArray()),
				new ReadOnlyCollection<YamlNode>(nodes.Flattened.ToArray()),
				new ReadOnlyCollection<string>(lines));
		}

		public void RemoveOpenFile(string fileUriString)
		{
			var uriString = OpenRaFolderUtils.NormalizeFileUriString(fileUriString);
			openFiles.Remove(uriString);
		}

		public bool ContainsFile(string fileUriString)
		{
			var uriString = OpenRaFolderUtils.NormalizeFileUriString(fileUriString);
			return openFiles.ContainsKey(uriString);
		}
	}
}
