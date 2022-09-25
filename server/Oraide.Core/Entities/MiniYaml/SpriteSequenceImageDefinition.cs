namespace Oraide.Core.Entities.MiniYaml
{
	public readonly struct SpriteSequenceImageDefinition
	{
		public readonly string Name;

		public readonly MemberLocation Location;

		public readonly SpriteSequenceDefinition[] Sequences;

		public SpriteSequenceImageDefinition(string name, MemberLocation location, SpriteSequenceDefinition[] sequences)
		{
			Name = name;
			Location = location;
			Sequences = sequences;
		}

		public override string ToString() => $"Image {Name}";

		public string ToMarkdownInfoString() => "```csharp\n" +
		                                        $"Image \"{Name}\"" +
		                                        "\n```";
	}
}
