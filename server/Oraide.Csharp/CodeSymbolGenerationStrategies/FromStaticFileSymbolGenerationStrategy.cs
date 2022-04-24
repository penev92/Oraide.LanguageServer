using System;
using System.Linq;
using Oraide.Core.Entities.Csharp;

namespace Oraide.Csharp.CodeSymbolGenerationStrategies
{
	class FromStaticFileSymbolGenerationStrategy : ICodeSymbolGenerationStrategy
	{
		public ILookup<string, TraitInfo> GetTraitInfos()
		{
			throw new NotImplementedException();
		}

		public WeaponInfo GetWeaponInfo()
		{
			throw new NotImplementedException();
		}
	}
}
