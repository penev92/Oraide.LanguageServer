using System.Linq;
using Oraide.Core.Entities.Csharp;

namespace Oraide.LanguageServer.Caching.Entities
{
	// TODO: Populate this asynchronously because it can be very, very slow.
	public class CodeSymbols
	{
		/// <summary>
		/// TraitInfo information grouped by TraitInfoName.
		/// </summary>
		public ILookup<string, ClassInfo> TraitInfos { get; }

		/// <summary>
		/// Palette TraitInfo information grouped by TraitInfoName.
		/// Palettes are just TraitInfos that have a name field with a PaletteDefinitionAttribute.
		/// </summary>
		public ILookup<string, ClassInfo> PaletteTraitInfos { get; }

		/// <summary>
		/// Information about the WeaponInfo class, all IProjectileInfo implementations and all IWarhead implementations.
		/// </summary>
		public WeaponInfo WeaponInfo { get; }

		/// <summary>
		/// SpriteSequence information grouped by name.
		/// </summary>
		public ILookup<string, ClassInfo> SpriteSequenceInfos { get; }

		/// <summary>
		/// Enum type information grouped by type name.
		/// </summary>
		public ILookup<string, EnumInfo> EnumInfos { get; }

		public CodeSymbols(ILookup<string, ClassInfo> traitInfos, ILookup<string, ClassInfo> paletteTraitInfos, WeaponInfo weaponInfo,
			ILookup<string, ClassInfo> spriteSequenceInfos, ILookup<string, EnumInfo> enumInfos)
		{
			TraitInfos = traitInfos;
			PaletteTraitInfos = paletteTraitInfos;
			WeaponInfo = weaponInfo;
			SpriteSequenceInfos = spriteSequenceInfos;
			EnumInfos = enumInfos;
		}
	}
}
