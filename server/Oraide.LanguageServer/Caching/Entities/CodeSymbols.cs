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

		public CodeSymbols(ILookup<string, TraitInfo> traitInfos, WeaponInfo weaponInfo)
		{
			TraitInfos = traitInfos;
			WeaponInfo = weaponInfo;
		}
	}
}
