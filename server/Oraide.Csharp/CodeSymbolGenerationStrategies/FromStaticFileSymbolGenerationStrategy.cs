using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Oraide.Core.Entities.Csharp;
using Oraide.Csharp.Abstraction.CodeSymbolGenerationStrategies;
using Oraide.Csharp.Abstraction.StaticFileParsers;
using Oraide.Csharp.StaticFileParsers;

namespace Oraide.Csharp.CodeSymbolGenerationStrategies
{
	class FromStaticFileSymbolGenerationStrategy : ICodeSymbolGenerationStrategy
	{
		readonly IStaticFileParser selectedParser;

		protected ILookup<string, ClassInfo> traitInfos;
		protected ILookup<string, ClassInfo> paletteTraitInfos;
		protected WeaponInfo weaponInfo;
		protected ILookup<string, ClassInfo> spriteSequenceInfos;
		protected ILookup<string, EnumInfo> enumInfos;
		protected ILookup<string, ClassInfo> assetLoaders;
		protected ILookup<string, ClassInfo> widgets;
		protected ILookup<string, ClassInfo> widgetLogicTypes;

		public string LoadedVersion { get; }

		public FromStaticFileSymbolGenerationStrategy(in string openRaFolder)
		{
			LoadedVersion = GetVersion(openRaFolder);
			var parsers = Assembly.GetExecutingAssembly().GetTypes()
				.Where(x => !x.IsAbstract && x.GetInterfaces().Contains(typeof(IStaticFileParser)))
				.Select(Activator.CreateInstance);

			selectedParser = (IStaticFileParser)parsers.FirstOrDefault(x => ((IStaticFileParser)x).EngineVersions.Contains(LoadedVersion));
			if (selectedParser == null)
			{
				LoadedVersion = $"Unsupported version ({LoadedVersion})";

				// Using the newest one and hoping for the best.
				selectedParser = new Release20230225StaticFileParser();
			}

			((BaseStaticFileParser)selectedParser).Load();
		}

		public ILookup<string, ClassInfo> GetTraitInfos()
		{
			if (traitInfos == null)
				traitInfos = selectedParser.ParseTraitInfos().ToLookup(x => x.Name, y => y);

			return traitInfos;
		}

		public ILookup<string, ClassInfo> GetPaletteTraitInfos()
		{
			if (paletteTraitInfos == null)
				paletteTraitInfos = selectedParser.ParsePaletteInfos().ToLookup(x => x.Name, y => y);

			return paletteTraitInfos;
		}

		public WeaponInfo GetWeaponInfo()
		{
			if (weaponInfo.WeaponPropertyInfos == null)
				weaponInfo = selectedParser.ParseWeaponInfo();

			return weaponInfo;
		}

		public ILookup<string, ClassInfo> GetSpriteSequenceInfos()
		{
			if (spriteSequenceInfos == null)
				spriteSequenceInfos = selectedParser.ParseSpriteSequenceInfos().ToLookup(x => x.Name, y => y);

			return spriteSequenceInfos;
		}

		public ILookup<string, EnumInfo> GetEnumInfos()
		{
			if (enumInfos == null)
				enumInfos = selectedParser.ParseEnumInfos().ToLookup(x => x.Name, y => y);

			return enumInfos;
		}

		public ILookup<string, ClassInfo> GetAssetLoaders()
		{
			if (assetLoaders == null)
				assetLoaders = selectedParser.ParseAssetLoaders().ToLookup(x =>
				{
					if (x.BaseTypes.Contains("IPackageLoader"))
						return "Package";

					if (x.BaseTypes.Contains("ISoundLoader"))
						return "Sound";

					if (x.BaseTypes.Contains("ISpriteLoader"))
						return "Sprite";

					if (x.BaseTypes.Contains("IVideoLoader"))
						return "Video";

					return string.Empty;
				}, y => y);

			return assetLoaders;
		}

		public ILookup<string, ClassInfo> GetWidgets()
		{
			if (widgets == null)
				widgets = selectedParser.ParseWidgets().ToLookup(x => x.Name, y => y);

			return widgets;
		}

		public ILookup<string, ClassInfo> GetWidgetLogicTypes()
		{
			if (widgetLogicTypes == null)
				widgetLogicTypes = selectedParser.ParseWidgetLogicTypes().ToLookup(x => x.NameWithTypeSuffix, y => y);

			return widgetLogicTypes;
		}

		#region Private methods

		string GetVersion(string openRaFolder)
		{
			var versionFile = Path.Combine(openRaFolder, "VERSION");
			return File.ReadAllText(versionFile).Trim();
		}

		#endregion
	}
}
