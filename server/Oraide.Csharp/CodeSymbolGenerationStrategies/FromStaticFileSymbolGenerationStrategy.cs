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
		readonly JObject traitsData;
		readonly JObject weaponsData;

		static readonly MemberLocation NoLocation = new MemberLocation(string.Empty, 0, 0);

		ILookup<string, TraitInfo> traitInfos;
		WeaponInfo weaponInfo;

		public FromStaticFileSymbolGenerationStrategy(string openRaFolder)
		{
			var version = GetVersion(openRaFolder);
			var assemblyLocation = Assembly.GetEntryAssembly().Location;
			var assemblyFolder = Path.GetDirectoryName(assemblyLocation);

			var traitsFile = Path.Combine(assemblyFolder, "docs", $"{version}-traits.json");
			var traitsText = File.ReadAllText(traitsFile);
			traitsData = JsonConvert.DeserializeObject<JObject>(traitsText);

			var weaponsFile = Path.Combine(assemblyFolder, "docs", $"{version}-weapons.json");
			var weaponsText = File.ReadAllText(weaponsFile);
			weaponsData = JsonConvert.DeserializeObject<JObject>(weaponsText);
		}

		public ILookup<string, TraitInfo> GetTraitInfos()
		{
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

		public WeaponInfo GetWeaponInfo()
		{
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

			weaponInfo = new WeaponInfo(typeInfos.FirstOrDefault(x => x.Name == "Weapon").PropertyInfos,
				typeInfos.Where(x => x.Name != "Weapon" && x.InheritedTypes.All(y => y != "Warhead")).ToArray(),  // Lame!
				typeInfos.Where(x => x.InheritedTypes.Any(y => y == "Warhead")).ToArray());

			return weaponInfo;
		}

		string GetVersion(string openRaFolder)
		{
			var versionFile = Path.Combine(openRaFolder, "VERSION");
			return File.ReadAllText(versionFile).Trim();
		}

		static ClassFieldInfo[] ReadProperties(JToken jToken)
		{
			return jToken["Properties"].Select(prop =>
			{
				var attributes = prop["OtherAttributes"]
					.Select(attribute =>
						(attribute["Name"].ToString(),
							attribute["Value"].ToString().Replace("\r\n", "").Replace(" ", "").Replace(",", ", ")))
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
	}
}
