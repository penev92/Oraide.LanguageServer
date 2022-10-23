using System.Linq;
using Oraide.Core.Entities.Csharp;
using Oraide.Csharp.CodeParsers;

namespace Oraide.Csharp.CodeSymbolGenerationStrategies
{
	class CodeParsingSymbolGenerationStrategy : ICodeSymbolGenerationStrategy
	{
		private readonly string openRaFolder;

		ILookup<string, TraitInfo> traitInfos;
		WeaponInfo weaponInfo;
		ILookup<string, TraitInfo> paletteTraitInfos;
		ILookup<string, SimpleClassInfo> spriteSequenceInfos;
		ILookup<string, EnumInfo> enumInfos;

		public CodeParsingSymbolGenerationStrategy(string openRaFolder)
		{
			this.openRaFolder = openRaFolder;
		}

		public ILookup<string, TraitInfo> GetTraitInfos()
		{
			if (traitInfos == null)
				Parse();

			return traitInfos;
		}

		public WeaponInfo GetWeaponInfo()
		{
			if (weaponInfo.WeaponPropertyInfos == null)
				Parse();

			return weaponInfo;
		}

		public ILookup<string, TraitInfo> GetPaletteTraitInfos()
		{
			if (paletteTraitInfos == null)
				Parse();

			return paletteTraitInfos;
		}

		public ILookup<string, SimpleClassInfo> GetSpriteSequenceInfos()
		{
			if (spriteSequenceInfos == null)
				Parse();

			return spriteSequenceInfos;
		}
		
		public ILookup<string, EnumInfo> GetEnums()
		{
			if (enumInfos == null)
				Parse();

			return enumInfos;
		}

		void Parse()
		{
			var (traits, weapons, palettes, spriteSequences, enums) = RoslynCodeParser.Parse(openRaFolder);
			traitInfos = traits;
			weaponInfo = weapons;
			paletteTraitInfos = palettes;
			spriteSequenceInfos = spriteSequences;
			enumInfos = enums;
		}
	}
}
