namespace Oraide.Core.Entities.MiniYaml
{
	public readonly struct ActorTraitDefinition
	{
		public string Name { get; }

		public string FullName { get; }

		public MemberLocation Location { get; }

		public ActorTraitDefinition(string name, MemberLocation location)
		{
			FullName = name;
			Name = name.Split('@')[0];
			Location = location;
		}
	}
}
