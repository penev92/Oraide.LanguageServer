using System.Collections.Generic;
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
		readonly IEnumerable<ICodeParser> codeParsers;

		ILookup<string, ClassInfo> traitInfos;
		ILookup<string, ClassInfo> paletteTraitInfos;
		WeaponInfo weaponInfo;
		ILookup<string, ClassInfo> spriteSequenceInfos;
		ILookup<string, EnumInfo> enumInfos;

		public string LoadedVersion { get; private set; }

		public CodeParsingSymbolGenerationStrategy(in string openRaFolder)
		{
			this.openRaFolder = openRaFolder;
			codeParsers = new ICodeParser[]
			{
				new Pre20210321RoslynCodeParser(),
				new BleedRoslynCodeParser()
			};
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
			foreach (var codeParser in codeParsers)
			{
				if (codeParser.CanParse(openRaFolder))
				{
					var codeInformation = codeParser.Parse(openRaFolder);

					LoadedVersion = codeParser.GetType().Name;

					traitInfos = codeInformation.TraitInfos;
					paletteTraitInfos = codeInformation.PaletteTraitInfos;
					weaponInfo = codeInformation.Weapons;
					spriteSequenceInfos = codeInformation.SpriteSequenceInfos;
					enumInfos = codeInformation.EnumInfos;

					return;
				}
			}
		}
	}
}
