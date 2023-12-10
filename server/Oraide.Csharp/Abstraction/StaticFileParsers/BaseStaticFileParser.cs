using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oraide.Core.Entities;
using Oraide.Core.Entities.Csharp;

namespace Oraide.Csharp.Abstraction.StaticFileParsers
{
	public abstract class BaseStaticFileParser : IStaticFileParser
	{
		public abstract string InternalVersionName { get; }

		protected static readonly MemberLocation NoLocation = new MemberLocation(string.Empty, 0, 0);

		protected JObject traitsData;
		protected JObject weaponsData;
		protected JObject spriteSequencesData;
		protected JObject assetLoadersData;
		protected JObject widgetsData;
		protected JObject widgetLogicData;

		public void Load()
		{
			var assemblyLocation = Assembly.GetEntryAssembly().Location;
			var assemblyFolder = Path.GetDirectoryName(assemblyLocation);

			traitsData = TryParseFile("traits");
			weaponsData = TryParseFile("weapons");
			spriteSequencesData = TryParseFile("sprite-sequences");
			assetLoadersData = TryParseFile("asset-loaders");
			widgetsData = TryParseFile("widgets");
			widgetLogicData = TryParseFile("widget-logic");

			JObject TryParseFile(string type)
			{
				var filePath = Path.Combine(assemblyFolder, "docs", InternalVersionName, $"{type}.json");
				var text = File.Exists(filePath) ? File.ReadAllText(filePath) : "";
				return JsonConvert.DeserializeObject<JObject>(text);
			}
		}

		#region IStaticFileParser implementation

		public abstract string[] EngineVersions { get; }

		public abstract IEnumerable<ClassInfo> ParseTraitInfos();

		public abstract IEnumerable<ClassInfo> ParsePaletteInfos();

		public abstract WeaponInfo ParseWeaponInfo();

		public abstract IEnumerable<ClassInfo> ParseSpriteSequenceInfos();

		public abstract IEnumerable<EnumInfo> ParseEnumInfos();

		public abstract IEnumerable<ClassInfo> ParseAssetLoaders();

		public abstract IEnumerable<ClassInfo> ParseWidgets();

		public abstract IEnumerable<ClassInfo> ParseWidgetLogicTypes();

		#endregion

		#region Protected methods

		protected ClassFieldInfo[] ReadProperties(JToken jToken)
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

		protected ClassFieldInfo[] ReadPropertiesWithoutValues(JToken jToken)
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
					null, string.Empty, NoLocation, prop["Description"].ToString(), attributes);

				return p;
			}).ToArray();
		}

		protected string[] GetBaseTypes(JToken jToken)
		{
			return jToken["InheritedTypes"]
				.Select(y => y.ToString())
				.ToArray();
		}

		protected string GetTypeNameWithoutSuffix(string fullName, ref string suffix)
		{
			if (fullName.EndsWith(suffix))
				return fullName.Substring(0, fullName.Length - suffix.Length);

			suffix = string.Empty;
			return fullName;
		}

		#endregion
	}
}
