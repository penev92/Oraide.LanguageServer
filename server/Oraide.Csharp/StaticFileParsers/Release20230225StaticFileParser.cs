using System;
using System.Collections.Generic;
using System.Linq;
using Oraide.Core.Entities.Csharp;
using Oraide.Csharp.Abstraction.StaticFileParsers;

namespace Oraide.Csharp.StaticFileParsers
{
	public class Release20230225StaticFileParser : BaseStaticFileParser
	{
		public override string InternalVersionName => "release-20230225";

		public override string[] EngineVersions { get; } =
		{
			"playtest-20221119",
			"playtest-20221203",
			"playtest-20221223",
			"playtest-20221224",
			"playtest-20230110",
			"release-20230225"
		};

		public override IEnumerable<ClassInfo> ParseTraitInfos()
		{
			var traits = traitsData["TraitInfos"]!.Select(x =>
			{
				var typeSuffix = "Info";
				var name = x["Name"].ToString();
				var baseTypes = GetBaseTypes(x);
				var properties = ReadProperties(x);

				return new ClassInfo(name, typeSuffix, x["Description"].ToString(),
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
				// This also handles Weapon and IProjectiles, but luckily none of those come with a suffix...
				var typeSuffix = "Warhead";
				var fullName = x["Name"].ToString();
				var name = GetTypeNameWithoutSuffix(fullName, ref typeSuffix);
				var baseTypes = GetBaseTypes(x);
				var properties = ReadProperties(x);

				return new ClassInfo(name, typeSuffix, x["Description"].ToString(),
					NoLocation, baseTypes, properties, false);
			}).ToArray();

			var weaponInfo = new WeaponInfo(typeInfos.FirstOrDefault(x => x.Name == "Weapon").PropertyInfos,
				typeInfos.Where(x => x.Name != "Weapon" && x.BaseTypes.All(y => y != "Warhead")).ToArray(),  // Lame!
				typeInfos.Where(x => x.BaseTypes.Any(y => y == "Warhead")).ToArray());

			return weaponInfo;
		}

		public override IEnumerable<ClassInfo> ParseSpriteSequenceInfos()
		{
			var typeInfos = spriteSequencesData["SpriteSequenceTypes"]!.Select(x =>
			{
				// SpriteSequence types come without a suffix.
				var typeSuffix = string.Empty;
				var name = x["Name"].ToString();
				var baseTypes = GetBaseTypes(x);
				var properties = ReadProperties(x);

				return new ClassInfo(name, typeSuffix, x["Description"].ToString(),
					NoLocation, baseTypes, properties, false);
			});

			return typeInfos;
		}

		public override IEnumerable<EnumInfo> ParseEnumInfos()
		{
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

			var spriteSequenceEnums = spriteSequencesData["RelatedEnums"]!.Select(x =>
			{
				return new EnumInfo(x["Name"].ToString(), $"{x["Namespace"]}.{x["Name"]}", null,
					x["Values"].Select(y => y["Value"].ToString()).ToArray(), false, NoLocation);
			});

			var widgetEnums = widgetsData["RelatedEnums"]!.Select(x =>
			{
				return new EnumInfo(x["Name"].ToString(), $"{x["Namespace"]}.{x["Name"]}", null,
					x["Values"].Select(y => y["Value"].ToString()).ToArray(), false, NoLocation);
			});

			return traitEnums.Union(weaponEnums).Union(spriteSequenceEnums).Union(widgetEnums);
		}

		public override IEnumerable<ClassInfo> ParseAssetLoaders()
		{
			var loaders = assetLoadersData["AssetLoaderTypes"]!.Select(x =>
			{
				var typeSuffix = "Loader";
				var fullName = x["Name"].ToString();
				var name = GetTypeNameWithoutSuffix(fullName, ref typeSuffix);
				var baseTypes = GetBaseTypes(x);

				return new ClassInfo(name, typeSuffix, x["Description"].ToString(),
					NoLocation, baseTypes, Array.Empty<ClassFieldInfo>(), false);
			});

			return loaders;
		}

		public override IEnumerable<ClassInfo> ParseWidgets()
		{
			var widgets = widgetsData["WidgetTypes"]!.Select(x =>
			{
				var typeSuffix = "Widget";
				var fullName = x["Name"].ToString();
				var name = GetTypeNameWithoutSuffix(fullName, ref typeSuffix);
				var baseTypes = GetBaseTypes(x);
				var properties = ReadPropertiesWithoutValues(x);

				return new ClassInfo(name, typeSuffix, x["Description"].ToString(),
					NoLocation, baseTypes, properties, false);
			});

			return widgets;
		}

		public override IEnumerable<ClassInfo> ParseWidgetLogicTypes()
		{
			var widgetLogicTypes = widgetLogicData["WidgetLogicTypes"]!.Select(x =>
			{
				var typeSuffix = "Logic";
				var fullName = x["Name"].ToString();
				var name = GetTypeNameWithoutSuffix(fullName, ref typeSuffix);
				var baseTypes = GetBaseTypes(x);

				return new ClassInfo(name, typeSuffix, x["Description"].ToString(),
					NoLocation, baseTypes, Array.Empty<ClassFieldInfo>(), false);
			});

			return widgetLogicTypes;
		}
	}
}
