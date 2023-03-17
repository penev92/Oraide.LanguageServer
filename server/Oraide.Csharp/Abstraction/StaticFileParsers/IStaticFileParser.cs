using System.Collections.Generic;
using Oraide.Core.Entities.Csharp;

namespace Oraide.Csharp.Abstraction.StaticFileParsers
{
	public interface IStaticFileParser
	{
		string[] EngineVersions { get; }

		IEnumerable<ClassInfo> ParseTraitInfos();

		IEnumerable<ClassInfo> ParsePaletteInfos();

		WeaponInfo ParseWeaponInfo();

		IEnumerable<ClassInfo> ParseSpriteSequenceInfos();

		IEnumerable<EnumInfo> ParseEnumInfos();

		IEnumerable<ClassInfo> ParseAssetLoaders();

		IEnumerable<ClassInfo> ParseWidgets();

		IEnumerable<ClassInfo> ParseWidgetLogicTypes();
	}
}
