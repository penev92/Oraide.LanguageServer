namespace Oraide.Core.Entities.MiniYaml
{
	public readonly struct ActorTraitDefinition
	{
		public readonly string NameWithIdentifier;

		public readonly string Name;

		public readonly string Value;

		// TODO:
		// public readonly ActorTraitPropertyDefinition[] Properties;

		public readonly MemberLocation Location;

		public ActorTraitDefinition(string nameWithIdentifier, string value, MemberLocation location)
		{
			NameWithIdentifier = nameWithIdentifier;
			Name = nameWithIdentifier.Split('@')[0];
			Value = value;
			Location = location;
		}

		public override string ToString() => NameWithIdentifier;
	}
}
