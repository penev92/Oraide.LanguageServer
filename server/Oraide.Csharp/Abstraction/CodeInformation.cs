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

		public readonly ILookup<string, ClassInfo> AssetLoaders;

		public CodeInformation(IEnumerable<ClassInfo> traitInfos, IEnumerable<ClassInfo> paletteTraitInfos, WeaponInfo weapons,
			IEnumerable<ClassInfo> spriteSequenceInfos, IEnumerable<EnumInfo> enumInfos, IEnumerable<ClassInfo> packageLoaders,
			IEnumerable<ClassInfo> soundLoaders, IEnumerable<ClassInfo> spriteLoaders, IEnumerable<ClassInfo> videoLoaders)
		{
			TraitInfos = traitInfos.ToLookup(x => x.NameWithTypeSuffix, y => y);
			PaletteTraitInfos = paletteTraitInfos.ToLookup(x => x.NameWithTypeSuffix, y => y);
			Weapons = weapons;
			SpriteSequenceInfos = spriteSequenceInfos.ToLookup(x => x.Name, y => y);
			EnumInfos = enumInfos.ToLookup(x => x.Name, y => y);

			IEnumerable<(string AssetType, ClassInfo ClassInfo)> assetLoaders = packageLoaders.Select(x => ("Package", x))
				.Union(soundLoaders.Select(x => ("Sound", x)))
				.Union(spriteLoaders.Select(x => ("Sprite", x)))
				.Union(videoLoaders.Select(x => ("Video", x)));

			AssetLoaders = assetLoaders.ToLookup(x => x.AssetType, y => y.ClassInfo);
		}
	}
}
