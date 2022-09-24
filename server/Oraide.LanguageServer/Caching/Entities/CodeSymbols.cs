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
		public ILookup<string, TraitInfo> TraitInfos { get; }

		/// <summary>
		/// Information about the WeaponInfo class, all IProjectileInfo implementations and all IWarhead implementations.
		/// </summary>
		public WeaponInfo WeaponInfo { get; }

		/// <summary>
		/// Palette TraitInfo information grouped by TraitInfoName.
		/// Palettes are just TraitInfos that have a name field with a PaletteDefinitionAttribute.
		/// </summary>
		public ILookup<string, TraitInfo> PaletteTraitInfos { get; }

		/// <summary>
		/// SpriteSequence information grouped by name.
		/// </summary>
		public ILookup<string, SimpleClassInfo> SpriteSequenceInfos { get; }
		
		/// <summary>
		/// Enum type information grouped by type name.
		/// </summary>
		public ILookup<string, EnumInfo> EnumInfos { get; }

		public CodeSymbols(ILookup<string, TraitInfo> traitInfos, WeaponInfo weaponInfo, ILookup<string, TraitInfo> paletteTraitInfos,
			ILookup<string, SimpleClassInfo> spriteSequenceInfos, ILookup<string, EnumInfo> enumInfos)
		{
			TraitInfos = traitInfos;
			WeaponInfo = weaponInfo;
			PaletteTraitInfos = paletteTraitInfos;
			SpriteSequenceInfos = spriteSequenceInfos;
			EnumInfos = enumInfos;
		}
	}
}
