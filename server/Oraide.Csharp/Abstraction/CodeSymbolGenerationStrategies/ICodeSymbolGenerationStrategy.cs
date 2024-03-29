﻿using System.Linq;
using Oraide.Core.Entities.Csharp;

namespace Oraide.Csharp.Abstraction.CodeSymbolGenerationStrategies
{
	public interface ICodeSymbolGenerationStrategy
	{
		string LoadedVersion { get; }

		ILookup<string, ClassInfo> GetTraitInfos();

		ILookup<string, ClassInfo> GetPaletteTraitInfos();

		WeaponInfo GetWeaponInfo();

		ILookup<string, ClassInfo> GetSpriteSequenceInfos();

		ILookup<string, EnumInfo> GetEnumInfos();

		ILookup<string, ClassInfo> GetAssetLoaders();

		ILookup<string, ClassInfo> GetWidgets();

		ILookup<string, ClassInfo> GetWidgetLogicTypes();
	}
}
