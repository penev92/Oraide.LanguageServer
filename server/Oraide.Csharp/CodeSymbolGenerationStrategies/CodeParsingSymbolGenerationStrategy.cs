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

		void Parse()
		{
			var (traits, weapons) = RoslynCodeParser.Parse(openRaFolder);
			traitInfos = traits;
			weaponInfo = weapons;
		}
	}
}
