using System.Collections.Generic;
using System.Linq;
using Oraide.Core.Entities.Csharp;

namespace Oraide.LanguageServer.Caching.Entities
{
	// TODO: Populate this asynchronously because it can be very, very slow.
	public class CodeSymbols
	{
		/// <summary>
		/// TraitInfo information grouped by trait name.
		/// </summary>
		public ILookup<string, ClassInfo> TraitInfos { get; }

		/// <summary>
		/// Palette TraitInfo information grouped by trait name.
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

		/// <summary>
		/// Asset loader types grouped by asset type (Package, Sound, Sprite, Video).
		/// </summary>
		public IReadOnlyDictionary<string, Dictionary<string, ClassInfo>> AssetLoaders { get; }

		/// <summary>
		/// Widget type information grouped by type name.
		/// </summary>
		public ILookup<string, ClassInfo> Widgets { get; }

		/// <summary>
		/// Widget logic type information grouped by type name with suffix.
		/// </summary>
		public ILookup<string, ClassInfo> WidgetLogicTypes { get; }

		public CodeSymbols(ILookup<string, ClassInfo> traitInfos, ILookup<string, ClassInfo> paletteTraitInfos, WeaponInfo weaponInfo,
			ILookup<string, ClassInfo> spriteSequenceInfos, ILookup<string, EnumInfo> enumInfos, ILookup<string, ClassInfo> assetLoaders,
			ILookup<string, ClassInfo> widgets, ILookup<string, ClassInfo> widgetLogicTypes)
		{
			TraitInfos = traitInfos;
			PaletteTraitInfos = paletteTraitInfos;
			WeaponInfo = weaponInfo;
			SpriteSequenceInfos = spriteSequenceInfos;
			EnumInfos = enumInfos;
			AssetLoaders = assetLoaders.ToDictionary(x => x.Key, y => y.ToDictionary(m => m.Name, n => n));
			Widgets = widgets;
			WidgetLogicTypes = widgetLogicTypes;
		}
	}
}
