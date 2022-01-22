namespace Oraide.Core.Entities.MiniYaml
{
	public struct WeaponDefinition
	{
		public string Name { get; set; }

		public MemberLocation Location { get; }

		public WeaponDefinition(string name, MemberLocation location)
		{
			Name = name;
			Location = location;
		}
	}
}
