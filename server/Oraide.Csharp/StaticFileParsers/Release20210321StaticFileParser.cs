using System.Collections.Generic;
using System.Linq;
using Oraide.Core.Entities.Csharp;
using Oraide.Csharp.Abstraction.StaticFileParsers;

namespace Oraide.Csharp.StaticFileParsers
{
	public class Release20210321StaticFileParser : BaseStaticFileParser
	{
		public override string EngineVersion => "release-20210321";

		public override IEnumerable<ClassInfo> ParseTraitInfos()
		{
			var traits = traitsData["TraitInfos"]!.Select(x =>
			{
				var baseTypes = GetBaseTypes(x);
				var properties = ReadProperties(x);

				return new ClassInfo(x["Name"].ToString(), $"{x["Name"]}Info", x["Description"].ToString(),
					NoLocation, baseTypes, properties, false);
			});

			return traits;
		}

		public override IEnumerable<ClassInfo> ParsePaletteInfos()
		{
			var traitInfos = ParseTraitInfos();

			// Palettes are just TraitInfos that have a name field with a PaletteDefinitionAttribute.
			return traitInfos
				.Where(x => x.PropertyInfos
					.Any(y => y.OtherAttributes
						.Any(z => z.Name == "PaletteDefinition")));
		}

		public override WeaponInfo ParseWeaponInfo()
		{
			var typeInfos = weaponsData["WeaponTypes"]!.Select(x =>
			{
				var baseTypes = GetBaseTypes(x);
				var properties = ReadProperties(x);

				var infoName = x["Name"].ToString();
				var name = infoName.EndsWith("Warhead") ? infoName.Substring(0, infoName.Length - 7) : infoName;
				return new ClassInfo(name, infoName, x["Description"].ToString(),
					NoLocation, baseTypes, properties, false);
			}).ToArray();

			var weaponInfo = new WeaponInfo(typeInfos.FirstOrDefault(x => x.Name == "Weapon").PropertyInfos,
				typeInfos.Where(x => x.Name != "Weapon" && x.BaseTypes.All(y => y != "Warhead")).ToArray(),  // Lame!
				typeInfos.Where(x => x.BaseTypes.Any(y => y == "Warhead")).ToArray());

			return weaponInfo;
		}

		public override IEnumerable<ClassInfo> ParseSpriteSequenceInfos()
		{
			// Release 20210321 does not support sprite sequence documentation.
			return Enumerable.Empty<ClassInfo>();
		}

		public override IEnumerable<EnumInfo> ParseEnumInfos()
		{
			// Release 20210321 does not support enum documentation.
			return Enumerable.Empty<EnumInfo>();
		}
	}
}
