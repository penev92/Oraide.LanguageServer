namespace Oraide.Core.Entities.MiniYaml
{
	public readonly struct ActorTraitDefinition
	{
		public readonly string NameWithIdentifier;

		public readonly string Name;

		// TODO:
		// public readonly ActorTraitPropertyDefinition[] Properties;

		public readonly MemberLocation Location;

		public ActorTraitDefinition(string nameWithIdentifier, MemberLocation location)
		{
			NameWithIdentifier = nameWithIdentifier;
			Name = nameWithIdentifier.Split('@')[0];
			Location = location;
		}

		public override string ToString() => NameWithIdentifier;
	}
}
