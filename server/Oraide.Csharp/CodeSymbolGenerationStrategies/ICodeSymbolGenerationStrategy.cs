using System.Linq;
using Oraide.Core.Entities.Csharp;

namespace Oraide.Csharp.CodeSymbolGenerationStrategies
{
	public interface ICodeSymbolGenerationStrategy
	{
		public ILookup<string, TraitInfo> GetTraitInfos();

		public WeaponInfo GetWeaponInfo();
	}
}
