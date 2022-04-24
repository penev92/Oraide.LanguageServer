using System;
using System.Linq;
using Oraide.Core.Entities.Csharp;
using Oraide.Csharp.CodeParsers;

namespace Oraide.Csharp
{
	abstract class CodeSymbolGenerationStrategy
	{
		public abstract ILookup<string, TraitInfo> GetTraitInfos();

		public abstract WeaponInfo GetWeaponInfo();
	}

	class CodeParsingSymbolGenerationStrategy : CodeSymbolGenerationStrategy
	{
		private readonly string openRaFolder;

		ILookup<string, TraitInfo> traitInfos;
		WeaponInfo weaponInfo;

		public CodeParsingSymbolGenerationStrategy(string openRaFolder)
		{
			this.openRaFolder = openRaFolder;
		}

		public override ILookup<string, TraitInfo> GetTraitInfos()
		{
			if (traitInfos == null)
				Parse();

			return traitInfos;
		}

		public override WeaponInfo GetWeaponInfo()
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

	class ReflectionSymbolGenerationStrategy : CodeSymbolGenerationStrategy
	{
		public override ILookup<string, TraitInfo> GetTraitInfos()
		{
			throw new NotImplementedException();
		}

		public override WeaponInfo GetWeaponInfo()
		{
			throw new NotImplementedException();
		}
	}

	class FromStaticFileSymbolGenerationStrategy : CodeSymbolGenerationStrategy
	{
		public override ILookup<string, TraitInfo> GetTraitInfos()
		{
			throw new NotImplementedException();
		}

		public override WeaponInfo GetWeaponInfo()
		{
			throw new NotImplementedException();
		}
	}
}
