using System;
using System.Collections.Generic;
using System.IO;
using Oraide.LanguageServer.CodeParsers;
using YamlDotNet.Serialization;

namespace Oraide.LanguageServer.YamlParsers
{
	public static class YamlDotNetYamlParser
	{
		public static void Parse(in string oraFolderPath)
		{
			ParseRules(Path.Combine(oraFolderPath, @"mods\d2k\rules"));
		}

		public static void ParseModFile()
		{
			var fileContents = File.ReadAllText(@"D:\Work.Personal\OpenRA\Oraide\Oraide.LanguageServer\Oraide.LanguageServer\mod2.yaml");
			fileContents = fileContents.Replace("\t", "  ");

			// var deserializer = new DeserializerBuilder().WithTypeConverter(new LoadScreenConverter()).Build();
			var deserializer = new Deserializer();
			var output = deserializer.Deserialize<MyModData>(fileContents);
		}

		public static void ParseRules(in string modFolderPath)
		{
			var actorDefinitions = new Dictionary<string, ActorDefinition>();

			var filePaths = Directory.EnumerateFiles(modFolderPath, "*.yaml", SearchOption.AllDirectories);
			foreach (var filePath in filePaths)
			{
				var fileContents = File.ReadAllText(filePath);
				fileContents = fileContents.Replace("\t", "  "); // YamlDotNet doesn't appreciate tabs for indentation.
				fileContents = fileContents.Replace("	  ", "	");  // YamlDotNet doesn't appreciate too much indentation.
				fileContents = fileContents.Replace("!", "");	// YamlDotNet doesn't appreciate !, so remove those from conditions used by traits.

				var deserializer = new Deserializer();
				var output = deserializer.Deserialize<Dictionary<string, object>>(fileContents);
				foreach (var node in output)
				{
					var actorDefinition = new ActorDefinition(node.Key, new MemberLocation(filePath, 0, 0), new List<TraitDefinition>());
					if (actorDefinitions.ContainsKey(node.Key))
						actorDefinitions.Add($"{node.Key}-{Guid.NewGuid()}", actorDefinition);
					else
						actorDefinitions.Add(node.Key, actorDefinition);
				}
			}

			Console.WriteLine(actorDefinitions.Count);
		}
	}
}
