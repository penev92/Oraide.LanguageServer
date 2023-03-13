using System.Linq;
using Oraide.Core.Entities.Csharp;
using Oraide.Csharp.Abstraction.CodeParsers;
using Oraide.Csharp.Abstraction.CodeSymbolGenerationStrategies;
using Oraide.Csharp.CodeParsers;

namespace Oraide.Csharp.CodeSymbolGenerationStrategies
{
	class CodeParsingSymbolGenerationStrategy : ICodeSymbolGenerationStrategy
	{
		readonly string openRaFolder;
		readonly ICodeParser selectedParser;

		ILookup<string, ClassInfo> traitInfos;
		ILookup<string, ClassInfo> paletteTraitInfos;
		WeaponInfo weaponInfo;
		ILookup<string, ClassInfo> spriteSequenceInfos;
		ILookup<string, EnumInfo> enumInfos;
		ILookup<string, ClassInfo> assetLoaders;
		ILookup<string, ClassInfo> widgets;

		public string LoadedVersion { get; }

		public CodeParsingSymbolGenerationStrategy(in string openRaFolder)
		{
			this.openRaFolder = openRaFolder;
			var codeParsers = new ICodeParser[]
			{
				new BleedRoslynCodeParser(),
				new Pre20210321RoslynCodeParser(),
			};

			selectedParser = codeParsers.First(x => x.CanParse(this.openRaFolder));
			LoadedVersion = selectedParser.GetType().Name;
		}

		public ILookup<string, ClassInfo> GetTraitInfos()
		{
			if (traitInfos == null)
				Parse();

			return traitInfos;
		}

		public ILookup<string, ClassInfo> GetPaletteTraitInfos()
		{
			if (paletteTraitInfos == null)
				Parse();

			return paletteTraitInfos;
		}

		public WeaponInfo GetWeaponInfo()
		{
			if (weaponInfo.WeaponPropertyInfos == null)
				Parse();

			return weaponInfo;
		}

		public ILookup<string, ClassInfo> GetSpriteSequenceInfos()
		{
			if (spriteSequenceInfos == null)
				Parse();

			return spriteSequenceInfos;
		}

		public ILookup<string, EnumInfo> GetEnumInfos()
		{
			if (enumInfos == null)
				Parse();

			return enumInfos;
		}

		public ILookup<string, ClassInfo> GetAssetLoaders()
		{
			if (assetLoaders == null)
				Parse();

			return assetLoaders;
		}

		public ILookup<string, ClassInfo> GetWidgets()
		{
			if (widgets == null)
				Parse();

			return widgets;
		}

		void Parse()
		{
			var codeInformation = selectedParser.Parse(openRaFolder);

			traitInfos = codeInformation.TraitInfos;
			paletteTraitInfos = codeInformation.PaletteTraitInfos;
			weaponInfo = codeInformation.Weapons;
			spriteSequenceInfos = codeInformation.SpriteSequenceInfos;
			enumInfos = codeInformation.EnumInfos;
			assetLoaders = codeInformation.AssetLoaders;
			widgets = codeInformation.Widgets;
		}
	}
}
