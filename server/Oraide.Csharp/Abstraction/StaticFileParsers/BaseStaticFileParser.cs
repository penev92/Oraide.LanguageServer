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
		protected static readonly MemberLocation NoLocation = new MemberLocation(string.Empty, 0, 0);

		protected JObject traitsData;
		protected JObject weaponsData;
		protected JObject spriteSequencesData;

		#region IStaticFileParser implementation

		public abstract string EngineVersion { get; }

		public void Load()
		{
			var assemblyLocation = Assembly.GetEntryAssembly().Location;
			var assemblyFolder = Path.GetDirectoryName(assemblyLocation);

			var traitsFile = Path.Combine(assemblyFolder, "docs", $"{EngineVersion}-traits.json");
			var weaponsFile = Path.Combine(assemblyFolder, "docs", $"{EngineVersion}-weapons.json");
			var spriteSequencesFile = Path.Combine(assemblyFolder, "docs", $"{EngineVersion}-sprite-sequences.json");

			var traitsText = File.Exists(traitsFile) ? File.ReadAllText(traitsFile) : "";
			traitsData = JsonConvert.DeserializeObject<JObject>(traitsText);

			var weaponsText = File.Exists(weaponsFile) ? File.ReadAllText(weaponsFile) : "";
			weaponsData = JsonConvert.DeserializeObject<JObject>(weaponsText);

			var spriteSequencesText = File.Exists(spriteSequencesFile) ? File.ReadAllText(spriteSequencesFile) : "";
			spriteSequencesData = JsonConvert.DeserializeObject<JObject>(spriteSequencesText);
		}

		public abstract IEnumerable<ClassInfo> ParseTraitInfos();

		public abstract IEnumerable<ClassInfo> ParsePaletteInfos();

		public abstract WeaponInfo ParseWeaponInfo();

		public abstract IEnumerable<ClassInfo> ParseSpriteSequenceInfos();

		public abstract IEnumerable<EnumInfo> ParseEnumInfos();

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

		protected string[] GetBaseTypes(JToken jToken)
		{
			return jToken["InheritedTypes"]
				.Select(y => y.ToString())
				.ToArray();
		}

		#endregion
	}
}
