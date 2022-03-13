namespace Oraide.Core.Entities.MiniYaml
{
	public readonly struct CursorDefinition
	{
		public readonly string Name;

		public readonly string SequenceFile;

		public readonly string Palette;

		public readonly MemberLocation Location;

		public CursorDefinition(string name, string sequenceFile, string palette, MemberLocation location)
		{
			Name = name;
			Palette = palette;
			SequenceFile = sequenceFile;
			Location = location;
		}

		public override string ToString() => Name;

		public string ToMarkdownInfoString() => "```csharp\n" +
		                                        $"Cursor \"{Name}\"" +
		                                        "\n```\n" +
		                                        $"File: **{SequenceFile}**  \n" +
		                                        $"Palette: **{Palette}**";
	}
}
