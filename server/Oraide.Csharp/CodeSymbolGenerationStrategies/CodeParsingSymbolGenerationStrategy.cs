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

		public string LoadedVersion { get; private set; }

		public CodeParsingSymbolGenerationStrategy(in string openRaFolder)
		{
			this.openRaFolder = openRaFolder;
			var codeParsers = new ICodeParser[]
			{
				new Pre20210321RoslynCodeParser(),
				new BleedRoslynCodeParser()
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

		void Parse()
		{
			var codeInformation = selectedParser.Parse(openRaFolder);

			traitInfos = codeInformation.TraitInfos;
			paletteTraitInfos = codeInformation.PaletteTraitInfos;
			weaponInfo = codeInformation.Weapons;
			spriteSequenceInfos = codeInformation.SpriteSequenceInfos;
			enumInfos = codeInformation.EnumInfos;
		}
	}
}
