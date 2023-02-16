﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oraide.Core.Entities;
using Oraide.Core.Entities.Csharp;

namespace Oraide.Csharp.CodeSymbolGenerationStrategies
{
	class FromStaticFileSymbolGenerationStrategy : ICodeSymbolGenerationStrategy
	{
		public readonly string LoadedVersion;

		readonly string traitsFile;
		readonly string weaponsFile;
		readonly string spriteSequencesFile;
		readonly JObject traitsData;
		readonly JObject weaponsData;
		readonly JObject spriteSequencesData;

		static readonly MemberLocation NoLocation = new MemberLocation(string.Empty, 0, 0);

		ILookup<string, TraitInfo> traitInfos;
		OldWeaponInfo weaponInfo;
		ILookup<string, TraitInfo> paletteTraitInfos;
		ILookup<string, SimpleClassInfo> spriteSequenceInfos;
		ILookup<string, EnumInfo> enumInfos;

		// TODO: Maybe create one of these per OpenRA version? The types of files and the format of the content may vary per release.
		public FromStaticFileSymbolGenerationStrategy(string openRaFolder)
		{
			LoadedVersion = GetVersion(openRaFolder);
			var assemblyLocation = Assembly.GetEntryAssembly().Location;
			var assemblyFolder = Path.GetDirectoryName(assemblyLocation);

			traitsFile = Path.Combine(assemblyFolder, "docs", $"{LoadedVersion}-traits.json");
			weaponsFile = Path.Combine(assemblyFolder, "docs", $"{LoadedVersion}-weapons.json");
			spriteSequencesFile = Path.Combine(assemblyFolder, "docs", $"{LoadedVersion}-sprite-sequences.json");

			var traitsText = File.Exists(traitsFile) ? File.ReadAllText(traitsFile) : "";
			traitsData = JsonConvert.DeserializeObject<JObject>(traitsText);

			var weaponsText = File.Exists(weaponsFile) ? File.ReadAllText(weaponsFile) : "";
			weaponsData = JsonConvert.DeserializeObject<JObject>(weaponsText);

			var spriteSequencesText = File.Exists(spriteSequencesFile) ? File.ReadAllText(spriteSequencesFile) : "";
			spriteSequencesData = JsonConvert.DeserializeObject<JObject>(spriteSequencesText);
		}

		public ILookup<string, TraitInfo> GetTraitInfos()
		{
			if (!File.Exists(traitsFile))
				return Array.Empty<TraitInfo>().ToLookup(x => x.TraitInfoName, y => y);

			if (traitInfos != null)
				return traitInfos;

			var traits = traitsData["TraitInfos"]!.Select(x =>
			{
				var baseTypes = GetBaseTypes(x);
				var properties = ReadProperties(x);

				return new TraitInfo(x["Name"].ToString(), $"{x["Name"]}Info", x["Description"].ToString(),
					NoLocation, baseTypes, properties, false);
			});

			traitInfos = traits.ToLookup(x => x.TraitInfoName, y => y);
			return traitInfos;
		}

		public OldWeaponInfo GetWeaponInfo()
		{
			if (!File.Exists(weaponsFile))
				return new OldWeaponInfo(Array.Empty<ClassFieldInfo>(), Array.Empty<SimpleClassInfo>(), Array.Empty<SimpleClassInfo>());

			if (weaponInfo.WeaponPropertyInfos != null)
				return weaponInfo;

			var typeInfos = weaponsData["WeaponTypes"]!.Select(x =>
			{
				var baseTypes = GetBaseTypes(x);
				var properties = ReadProperties(x);

				var infoName = x["Name"].ToString();
				var name = infoName.EndsWith("Warhead") ? infoName.Substring(0, infoName.Length - 7) : infoName;
				return new SimpleClassInfo(name, infoName, x["Description"].ToString(),
					NoLocation, baseTypes, properties, false);
			}).ToArray();

			weaponInfo = new OldWeaponInfo(typeInfos.FirstOrDefault(x => x.Name == "Weapon").PropertyInfos,
				typeInfos.Where(x => x.Name != "Weapon" && x.InheritedTypes.All(y => y != "Warhead")).ToArray(),  // Lame!
				typeInfos.Where(x => x.InheritedTypes.Any(y => y == "Warhead")).ToArray());

			return weaponInfo;
		}

		public ILookup<string, TraitInfo> GetPaletteTraitInfos()
		{
			if (!File.Exists(traitsFile))
				return Array.Empty<TraitInfo>().ToLookup(x => x.TraitInfoName, y => y);

			if (paletteTraitInfos != null)
				return paletteTraitInfos;

			if (traitInfos == null)
				GetTraitInfos();

			// Palettes are just TraitInfos that have a name field with a PaletteDefinitionAttribute.
			paletteTraitInfos = traitInfos
				.Where(x => x
					.Any(y => y.TraitPropertyInfos
						.Any(z => z.OtherAttributes
							.Any(a => a.Name == "PaletteDefinition"))))
				.SelectMany(x => x)
				.ToLookup(x => x.TraitInfoName, y => y);

			return paletteTraitInfos;
		}

		public ILookup<string, SimpleClassInfo> GetSpriteSequenceInfos()
		{
			if (!File.Exists(spriteSequencesFile))
				return Array.Empty<SimpleClassInfo>().ToLookup(x => x.Name, y => y);

			if (spriteSequenceInfos != null)
				return spriteSequenceInfos;

			var typeInfos = spriteSequencesData["SpriteSequenceTypes"]!.Select(x =>
			{
				var baseTypes = GetBaseTypes(x);
				var properties = ReadProperties(x);

				var name = x["Name"].ToString();
				return new SimpleClassInfo(name, name, x["Description"].ToString(),
					NoLocation, baseTypes, properties, false);
			});

			return typeInfos.ToLookup(x => x.Name, y => y);
		}

		public ILookup<string, EnumInfo> GetEnums()
		{
			if (!File.Exists(traitsFile) || !File.Exists(weaponsFile))
				return Array.Empty<EnumInfo>().ToLookup(x => x.Name, y => y);

			if (enumInfos != null)
				return enumInfos;

			if (traitInfos == null)
				GetTraitInfos();

			if (weaponInfo.WeaponPropertyInfos == null)
				GetWeaponInfo();

			var traitEnums = traitsData["RelatedEnums"]!.Select(x =>
			{
				return new EnumInfo(x["Name"].ToString(), $"{x["Namespace"]}.{x["Name"]}", null,
					x["Values"].Select(y => y["Value"].ToString()).ToArray(), false, NoLocation);
			});

			var weaponEnums = weaponsData["RelatedEnums"]!.Select(x =>
			{
				return new EnumInfo(x["Name"].ToString(), $"{x["Namespace"]}.{x["Name"]}", null,
					x["Values"].Select(y => y["Value"].ToString()).ToArray(), false, NoLocation);
			});

			enumInfos = traitEnums.Union(weaponEnums).ToLookup(x => x.Name, y => y);
			return enumInfos;
		}

		#region Private methods

		string GetVersion(string openRaFolder)
		{
			var versionFile = Path.Combine(openRaFolder, "VERSION");
			return File.ReadAllText(versionFile).Trim();
		}

		static ClassFieldInfo[] ReadProperties(JToken jToken)
		{
			return jToken["Properties"].Select(prop =>
			{
				var attributes = prop["OtherAttributes"] == null
					? Array.Empty<(string nameof, string Value)>()
					: prop["OtherAttributes"]
						.Select(attribute =>
							(attribute["Name"].ToString(), ""))
						.ToArray();

				var p = new ClassFieldInfo(prop["PropertyName"].ToString(), prop["InternalType"].ToString(), prop["UserFriendlyType"].ToString(),
					prop["DefaultValue"].ToString(), string.Empty, NoLocation, prop["Description"].ToString(), attributes);

				return p;
			}).ToArray();
		}

		static string[] GetBaseTypes(JToken jToken)
		{
			return jToken["InheritedTypes"]
				.Select(y => y.ToString())
				.ToArray();
		}

		#endregion
	}
}
