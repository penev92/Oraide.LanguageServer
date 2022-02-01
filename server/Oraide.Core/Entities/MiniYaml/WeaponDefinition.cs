namespace Oraide.Core.Entities.MiniYaml
{
	public struct WeaponDefinition
	{
		public string Name { get; set; }

		public WeaponProjectileDefinition Projectile { get; }

		public WeaponWarheadDefinition[] Warheads { get; }

		public MemberLocation Location { get; }

		public WeaponDefinition(string name, WeaponProjectileDefinition projectile, WeaponWarheadDefinition[] warheads, MemberLocation location)
		{
			Name = name;
			Projectile = projectile;
			Warheads = warheads;
			Location = location;
		}
	}
}
