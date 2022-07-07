namespace Oraide.Core.Entities.MiniYaml
{
	public readonly struct PaletteDefinition
	{
		public readonly string Name;

		public readonly string FileName;

		public readonly string BasePalette;

		public readonly string Identifier;

		public readonly string Type;

		public readonly MemberLocation Location;

		public PaletteDefinition(string name, string fileName, string basePalette, string identifier, string type, MemberLocation location)
		{
			Name = name;
			FileName = fileName;
			BasePalette = basePalette;
			Identifier = identifier;
			Type = type;
			Location = location;
		}

		public override string ToString() => Name;

		public string ToMarkdownInfoString() => "```csharp\n" +
		                                        $"Palette \"{Name}\"" +
		                                        "\n```\n" +
		                                        $"Type: **{Type}**";
	}
}
