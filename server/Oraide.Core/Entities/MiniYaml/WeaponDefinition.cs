namespace Oraide.Core.Entities.MiniYaml
{
	public readonly struct WeaponDefinition
	{
		public readonly string Name;

		public readonly WeaponProjectileDefinition Projectile;

		public readonly WeaponWarheadDefinition[] Warheads;

		public readonly MemberLocation Location;

		public WeaponDefinition(string name, WeaponProjectileDefinition projectile, WeaponWarheadDefinition[] warheads, MemberLocation location)
		{
			Name = name;
			Projectile = projectile;
			Warheads = warheads;
			Location = location;
		}

		public override string ToString() => $"{nameof(WeaponDefinition)}: {Name}, {Projectile.Name}, {Warheads.Length} Warheads";
	}
}
