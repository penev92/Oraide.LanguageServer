namespace Oraide.Core.Entities.Csharp
{
	/// <summary>
	/// Represents information about OpenRA's WeaponInfo class and related classes - implementations of IProjectile and IWarhead.
	/// Holds information about the C# side of things, to be used for navigation to code and for autocomplete.
	/// </summary>
	public readonly struct WeaponInfo
	{
		// WeaponInfo properties.
		public readonly ClassFieldInfo[] WeaponPropertyInfos;

		// List of all implementations of IProjectileInfo and their properties.
		public readonly ClassInfo[] ProjectileInfos;

		// List of all implementations of IWarhead and their properties.
		public readonly ClassInfo[] WarheadInfos;

		public WeaponInfo(ClassFieldInfo[] weaponPropertyInfos, ClassInfo[] projectileInfos, ClassInfo[] warheadInfos)
		{
			WeaponPropertyInfos = weaponPropertyInfos;
			ProjectileInfos = projectileInfos;
			WarheadInfos = warheadInfos;
		}
	}
}
