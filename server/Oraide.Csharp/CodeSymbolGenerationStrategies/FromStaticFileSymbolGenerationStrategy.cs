using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Oraide.Core.Entities.Csharp;
using Oraide.Csharp.Abstraction.CodeSymbolGenerationStrategies;
using Oraide.Csharp.Abstraction.StaticFileParsers;

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

		public string LoadedVersion { get; }

		public FromStaticFileSymbolGenerationStrategy(in string openRaFolder)
		{
			LoadedVersion = GetVersion(openRaFolder);
			var parsers = Assembly.GetExecutingAssembly().GetTypes()
				.Where(x => x.GetInterfaces().Contains(typeof(IStaticFileParser)))
				.Select(Activator.CreateInstance);

			selectedParser = (IStaticFileParser)parsers.FirstOrDefault(x => ((IStaticFileParser)x).EngineVersions.Contains(LoadedVersion));
			((BaseStaticFileParser)selectedParser).Load();
		}

		public ILookup<string, ClassInfo> GetTraitInfos()
		{
			if (traitInfos == null)
				traitInfos = selectedParser.ParseTraitInfos().ToLookup(x => x.InfoName, y => y);

			return traitInfos;
		}

		public ILookup<string, ClassInfo> GetPaletteTraitInfos()
		{
			if (paletteTraitInfos == null)
				paletteTraitInfos = selectedParser.ParsePaletteInfos().ToLookup(x => x.InfoName, y => y);

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

		#region Private methods

		string GetVersion(string openRaFolder)
		{
			var versionFile = Path.Combine(openRaFolder, "VERSION");
			return File.ReadAllText(versionFile).Trim();
		}

		#endregion
	}
}
