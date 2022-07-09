namespace Oraide.Core.Entities.MiniYaml
{
	public readonly struct WeaponWarheadDefinition
	{
		public readonly string Name;

		public readonly MemberLocation Location;

		public WeaponWarheadDefinition(string name, MemberLocation location)
		{
			Name = name;
			Location = location;
		}

		public override string ToString() => $"Warhead {Name}";
	}
}
