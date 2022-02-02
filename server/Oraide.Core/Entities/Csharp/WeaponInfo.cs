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
		public readonly SimpleClassInfo[] ProjectileInfos;

		// List of all implementations of IWarhead and their properties.
		public readonly SimpleClassInfo[] WarheadInfos;

		public WeaponInfo(ClassFieldInfo[] weaponPropertyInfos, SimpleClassInfo[] projectileInfos, SimpleClassInfo[] warheadInfos)
		{
			WeaponPropertyInfos = weaponPropertyInfos;
			ProjectileInfos = projectileInfos;
			WarheadInfos = warheadInfos;
		}
	}
}
