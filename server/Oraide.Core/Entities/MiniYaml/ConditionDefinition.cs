namespace Oraide.Core.Entities.MiniYaml
{
	public struct ConditionDefinition
	{
		public string Name { get; set; }

		public MemberLocation Location { get; }

		public ConditionDefinition(string name, MemberLocation location)
		{
			Name = name;
			Location = location;
		}
	}
}
