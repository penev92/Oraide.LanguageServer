using System;
using System.Collections.Generic;
using System.IO;
using Oraide.LanguageServer.CodeParsers;

namespace Oraide.LanguageServer.YamlParsers
{
	public static class ManualYamlParser
	{
		public static void Parse(in string oraFolderPath)
		{
			ParseRules(Path.Combine(oraFolderPath, @"mods\d2k\rules"));
		}

		public static Dictionary<string, ActorDefinition> ParseRules(in string modFolderPath)
		{
			var currentActorTraits = new List<TraitDefinition>();
			var actorDefinitions = new Dictionary<string, ActorDefinition>();

			var filePaths = Directory.EnumerateFiles(modFolderPath, "*.yaml", SearchOption.AllDirectories);
			foreach (var filePath in filePaths)
			{

				var fileLines = File.ReadAllLines(filePath);
				for (var i = 0; i < fileLines.Length; i++)
				{
					var line = fileLines[i];
					if (string.IsNullOrWhiteSpace(line))
						continue;

					if (!line.StartsWith('\t'))
					{
						var key = line.Substring(0, line.IndexOf(':'));
						var keyLocation  = new MemberLocation(filePath, i, 0);

						currentActorTraits = new List<TraitDefinition>();
						var currentActorDefinition = new ActorDefinition(key, keyLocation, currentActorTraits);

						if (actorDefinitions.ContainsKey(key))
							key = $"{key}:{Guid.NewGuid()}";

						actorDefinitions.Add(key, currentActorDefinition);
					}

					if (line.StartsWith('\t') && char.IsLetter(line[1]))
					{
						var key = line.Trim().Split(':')[0];
						currentActorTraits.Add(new TraitDefinition(key, new MemberLocation(filePath, i, 1)));
					}
				}
			}

			return actorDefinitions;
		}
	}
}

//  - List of actor definitions, each with its location, to be able to jump to a definition. (only from Inherits:)
//  - All files as arrays of parsed entities (actor/trait/property) to make lookups by line number fast af. (to easily find what the IDE/client is referencing on every request)
//
//  - Parse everything as YamlNodes?
//  Level1 nodes (top-level nodes) are either actor or weapon definitions. They can only reference other top-level nodes.
//  Level2 nodes (one-tab indentation) are traits for actors, but properties for weapons...
//	For actors they reference TraitInfos. Their values can reference top-level nodes.
//	For weapons they reference WeaponInfo properties. Their values can reference top-level nodes, an IProjectileInfo implementation or an IWarhead implementation.
//  Level3 nodes (two-tab indentation) are trait properties for actors,
//	For actors they are trait properties. Their values can reference top-level nodes, condition string literals or a name string literal defined by another Level3 node.
//	For weapons they are either ProjectileInfo or Warhead properties.
//
//
//  Use cases:
//	User opens OpenRA folder.	( ./OpenRA/ )
//	User opens mods folder.	( ./OpenRA/mods/ )
//	User opens a single mod's folder.	( ./OpenRA/mods/d2k/ )
//	User opens a subfolder of any mod.	( ./OpenRA/mods/d2k/rules/ )
