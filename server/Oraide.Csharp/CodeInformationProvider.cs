﻿using System;
using System.IO;
using System.Linq;
using Oraide.Core;
using Oraide.Core.Entities.Csharp;
using Oraide.Csharp.Abstraction.CodeSymbolGenerationStrategies;
using Oraide.Csharp.CodeSymbolGenerationStrategies;

namespace Oraide.Csharp
{
	// The currently planned/supported use-cases for code information are:
	//  - Generating symbols to navigate to.
	//  - Referencing documentation from DescAttributes.
	// Future planned use-cases:
	//  - Validating trait and trait property existence (using the same symbols used for navigation).
	//  - Autocomplete for trait and trait property names.
	//  - Any of the above for non-trait types.
	public class CodeInformationProvider
	{
		readonly ICodeSymbolGenerationStrategy symbolGenerator;

		public CodeInformationProvider(string workspaceFolderPath, string defaultOpenRaFolderPath)
		{
			Console.Error.WriteLine($"WORKSPACE FOLDER PATH:  {workspaceFolderPath}");
			Console.Error.WriteLine($"OPENRA DEFAULT FOLDER PATH:  {defaultOpenRaFolderPath}");

			var openRaFolder = GetOpenRaFolder(workspaceFolderPath, defaultOpenRaFolderPath);

			Console.Error.WriteLine($"OPENRA CHOSEN FOLDER PATH:  {openRaFolder}");
			Console.Error.WriteLine("-------------");

			if (OpenRaFolderUtils.IsOpenRaRepositoryFolder(openRaFolder) || OpenRaFolderUtils.IsModSdkRepositoryFolder(openRaFolder))
			{
				// Strategy 1 - C# code parsing.
				symbolGenerator = new CodeParsingSymbolGenerationStrategy(openRaFolder);

				Console.Error.WriteLine($"Loading code symbols from source code files using {symbolGenerator.LoadedVersion}.");
				Console.Error.WriteLine("-------------");
			}
			else if (OpenRaFolderUtils.IsOpenRaInstallationFolder(openRaFolder))
			{
				// Strategy 2 - load data from static file.
				symbolGenerator = new FromStaticFileSymbolGenerationStrategy(openRaFolder);

				Console.Error.WriteLine($"Loading code symbols from static docs files - version {symbolGenerator.LoadedVersion}.");
				Console.Error.WriteLine("-------------");
			}
		}

		public ILookup<string, TraitInfo> GetTraitInfos()
		{
			return null;// symbolGenerator.GetTraitInfos();
		}

		public ILookup<string, TraitInfo> GetPaletteTraitInfos()
		{
			return null;//symbolGenerator.GetPaletteTraitInfos();
		}

		public OldWeaponInfo GetWeaponInfo()
		{
			return default;//symbolGenerator.GetWeaponInfo();
		}

		public ILookup<string, SimpleClassInfo> GetSpriteSequenceInfos()
		{
			return null;//symbolGenerator.GetSpriteSequenceInfos();
		}

		public ILookup<string, EnumInfo> GetEnumInfos()
		{
			return null;//symbolGenerator.GetEnums();
		}

		string GetOpenRaFolder(string workspaceFolderPath, string defaultOpenRaFolderPath)
		{
			var oraFolderPath = "";
			if (OpenRaFolderUtils.IsOpenRaFolder(workspaceFolderPath))
				oraFolderPath = workspaceFolderPath;
			else if (OpenRaFolderUtils.IsModsFolder(workspaceFolderPath))
			{
				var parentFolder = Directory.GetParent(workspaceFolderPath)?.FullName;
				if (OpenRaFolderUtils.IsOpenRaFolder(parentFolder))
					oraFolderPath = parentFolder;
			}
			else if (OpenRaFolderUtils.IsModFolder(workspaceFolderPath))
			{
				var parentFolder = Directory.GetParent(workspaceFolderPath)?.Parent?.FullName;
				if (OpenRaFolderUtils.IsOpenRaFolder(parentFolder))
					oraFolderPath = parentFolder;
			}

			if (string.IsNullOrEmpty(oraFolderPath))
			{
				if (OpenRaFolderUtils.IsOpenRaFolder(defaultOpenRaFolderPath))
					oraFolderPath = defaultOpenRaFolderPath;
			}

			return oraFolderPath;
		}
	}
}
