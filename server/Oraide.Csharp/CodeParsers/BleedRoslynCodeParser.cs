using System.IO;
using System.Linq;
using Oraide.Csharp.Abstraction.CodeParsers;

namespace Oraide.Csharp.CodeParsers
{
	/// <summary>
	/// Verified to be able to load symbols from engine versions from release-20210321 to prep-2212 to 2023.02.16 bleed, including the OpenRA repo and ModSDK-based repos.
	/// </summary>
	public class BleedRoslynCodeParser : BaseRoslynCodeParser
	{
		public override bool CanParse(in string oraFolderPath)
		{
			try
			{
				// The distinguishing change was removing ITraitInfo, so using a random (the first possible) file to check if it is there.
				var file = Directory.EnumerateFiles(oraFolderPath, "DebugPauseState.cs", SearchOption.AllDirectories).Single();
				return File.ReadAllText(file).Contains("DebugPauseStateInfo : TraitInfo");
			}
			catch
			{
				return false;
			}
		}
	}
}
