using System;
using System.Linq;
using Oraide.Core.Entities.Csharp;
using Oraide.Csharp.CodeParsers;

namespace Oraide.Csharp
{
	abstract class CodeSymbolGenerationStrategy
	{
		public abstract ILookup<string, TraitInfo> GetTraitInfos(string openRaFolder);

		public abstract WeaponInfo GetWeaponInfo(string openRaFolder);
	}

	class CodeParsingSymbolGenerationStrategy : CodeSymbolGenerationStrategy
	{
		ILookup<string, TraitInfo> traitInfos;
		WeaponInfo weaponInfo;

		public override ILookup<string, TraitInfo> GetTraitInfos(string openRaFolder)
		{
			if (traitInfos == null)
				Parse(openRaFolder);

			return traitInfos;
		}

		public override WeaponInfo GetWeaponInfo(string openRaFolder)
		{
			if (weaponInfo == null)
				Parse(openRaFolder);

			return weaponInfo;
		}

		void Parse(string openRaFolder)
		{
			var (traits, weapons) = RoslynCodeParser.Parse(openRaFolder);
			traitInfos = traits;
			weaponInfo = weapons;
		}
	}

	class ReflectionSymbolGenerationStrategy : CodeSymbolGenerationStrategy
	{
		public override ILookup<string, TraitInfo> GetTraitInfos(string openRaFolder)
		{
			throw new NotImplementedException();
		}

		public override WeaponInfo GetWeaponInfo(string openRaFolder)
		{
			throw new NotImplementedException();
		}
	}

	class FromStaticFileSymbolGenerationStrategy : CodeSymbolGenerationStrategy
	{
		public override ILookup<string, TraitInfo> GetTraitInfos(string openRaFolder)
		{
			throw new NotImplementedException();
		}

		public override WeaponInfo GetWeaponInfo(string openRaFolder)
		{
			throw new NotImplementedException();
		}
	}
}
