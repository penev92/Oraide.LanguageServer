using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Oraide.Core
{
	public static class OpenRaFolderUtils
	{
		const string ModFile = "mod.yaml";
		const string MakeFile = "Makefile";
		const string VersionFile = "VERSION";
		const string ModConfigFile = "mod.config";
		const string UtilityExeFile = "OpenRA.Utility.exe";

		public static bool IsModFolder(string folderPath)
		{
			// Contains "mod.yaml" file.
			return File.Exists(Path.Combine(folderPath, ModFile));
		}

		public static bool IsModsFolder(string folderPath)
		{
			return Directory.EnumerateDirectories(folderPath).Any(IsModFolder);
		}

		// Combines IsOpenRaRepositoryFolder, IsModSdkRepositoryFolder and IsOpenRaInstallationFolder.
		public static bool IsOpenRaFolder(string folderPath)
		{
			var modsFolder = Path.Combine(folderPath, "mods");

			// Contains "Makefile" and "VERSION" files, contains an IsModsFolder().
			return (File.Exists(Path.Combine(folderPath, ModConfigFile)) ||
			       File.Exists(Path.Combine(folderPath, VersionFile))) &&
			       Directory.Exists(modsFolder) &&
			       IsModsFolder(modsFolder);
		}

		public static bool IsOpenRaRepositoryFolder(string folderPath)
		{
			var modsFolder = Path.Combine(folderPath, "mods");

			// Contains "Makefile" and "VERSION" files, contains an IsModsFolder().
			return File.Exists(Path.Combine(folderPath, MakeFile)) &&
			       File.Exists(Path.Combine(folderPath, VersionFile)) &&
			       Directory.Exists(modsFolder) &&
			       IsModsFolder(modsFolder);
		}

		public static bool IsModSdkRepositoryFolder(string folderPath)
		{
			var modsFolder = Path.Combine(folderPath, "mods");

			// Contains "Makefile" and "mod.config" files, contains an IsModsFolder().
			return File.Exists(Path.Combine(folderPath, MakeFile)) &&
			       File.Exists(Path.Combine(folderPath, ModConfigFile)) &&
			       Directory.Exists(modsFolder) &&
			       IsModsFolder(modsFolder);
		}

		public static bool IsOpenRaInstallationFolder(string folderPath)
		{
			var modsFolder = Path.Combine(folderPath, "mods");

			// Contains "OpenRA.Utility.exe" and "VERSION" files, contains an IsModsFolder().
			return File.Exists(Path.Combine(folderPath, UtilityExeFile)) &&
			       File.Exists(Path.Combine(folderPath, VersionFile)) &&
			       Directory.Exists(modsFolder) &&
			       IsModsFolder(modsFolder);
		}

		public static string GetModId(string filePath)
		{
			var fileUri = filePath.Replace("\\", "/");
			var match = Regex.Match(fileUri, "(\\/mods\\/[^\\/]*\\/)").Value;
			if (match == string.Empty)
				match = Regex.Match(fileUri, "(\\/mods\\/[^\\/]*$)").Value;

			return match.Split('/')[2];
		}
	}
}
