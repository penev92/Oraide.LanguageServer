namespace Oraide.Core.Entities.MiniYaml
{
	public struct WeaponProjectileDefinition
	{
		public string Name { get; set; }

		public MemberLocation Location { get; }

		public WeaponProjectileDefinition(string name, MemberLocation location)
		{
			Name = name;
			Location = location;
		}
	}
}
