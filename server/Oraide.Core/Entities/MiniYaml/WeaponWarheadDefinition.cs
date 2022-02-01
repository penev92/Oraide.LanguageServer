namespace Oraide.Core.Entities.MiniYaml
{
	public struct WeaponWarheadDefinition
	{
		public string Name { get; set; }

		public MemberLocation Location { get; }

		public WeaponWarheadDefinition(string name, MemberLocation location)
		{
			Name = name;
			Location = location;
		}
	}
}
