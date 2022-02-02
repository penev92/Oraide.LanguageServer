namespace Oraide.Core.Entities.MiniYaml
{
	public readonly struct WeaponProjectileDefinition
	{
		public readonly string Name;

		public readonly MemberLocation Location;

		public WeaponProjectileDefinition(string name, MemberLocation location)
		{
			Name = name;
			Location = location;
		}
	}
}
