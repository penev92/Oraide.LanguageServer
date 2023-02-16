using System.Collections.Generic;
using System.Linq;
using Oraide.Core.Entities.Csharp;

namespace Oraide.Csharp.Abstraction
{
	public readonly struct CodeInformation
	{
		public readonly ILookup<string, ClassInfo> TraitInfos;

		public readonly ILookup<string, ClassInfo> PaletteTraitInfos;

		public readonly WeaponInfo Weapons;

		public readonly ILookup<string, ClassInfo> SpriteSequenceInfos;

		public readonly ILookup<string, EnumInfo> EnumInfos;

		public CodeInformation(IEnumerable<ClassInfo> traitInfos, IEnumerable<ClassInfo> paletteTraitInfos, WeaponInfo weapons,
			IEnumerable<ClassInfo> spriteSequenceInfos, IEnumerable<EnumInfo> enumInfos)
		{
			TraitInfos = traitInfos.ToLookup(x => x.InfoName, y => y);
			PaletteTraitInfos = paletteTraitInfos.ToLookup(x => x.InfoName, y => y);
			Weapons = weapons;
			SpriteSequenceInfos = spriteSequenceInfos.ToLookup(x => x.Name, y => y);
			EnumInfos = enumInfos.ToLookup(x => x.Name, y => y);
		}
	}
}
