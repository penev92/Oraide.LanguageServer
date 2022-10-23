using System.Linq;
using Oraide.Core.Entities.Csharp;

namespace Oraide.Csharp.CodeSymbolGenerationStrategies
{
	public interface ICodeSymbolGenerationStrategy
	{
		ILookup<string, TraitInfo> GetTraitInfos();

		WeaponInfo GetWeaponInfo();

		ILookup<string, TraitInfo> GetPaletteTraitInfos();

		ILookup<string, SimpleClassInfo> GetSpriteSequenceInfos();
		
		ILookup<string, EnumInfo> GetEnums();
	}
}
