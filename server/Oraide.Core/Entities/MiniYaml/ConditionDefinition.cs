namespace Oraide.Core.Entities.MiniYaml
{
	public readonly struct ConditionDefinition
	{
		public readonly string Name;

		public readonly MemberLocation Location;

		public ConditionDefinition(string name, MemberLocation location)
		{
			Name = name;
			Location = location;
		}
	}
}
