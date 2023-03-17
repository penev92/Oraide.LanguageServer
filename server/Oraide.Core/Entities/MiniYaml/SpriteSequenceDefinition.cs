namespace Oraide.Core.Entities.MiniYaml
{
	public readonly struct SpriteSequenceDefinition
	{
		public readonly string Name;

		public readonly string ImageName;

		// TODO: This is obsolete on bleed since 12.2022! Keeping here only for backward compatibility.
		public readonly string FileName;

		public readonly MemberLocation Location;

		public SpriteSequenceDefinition(string name, string imageName, string fileName, MemberLocation location)
		{
			Name = name;
			ImageName = imageName;
			FileName = fileName;
			Location = location;
		}

		public override string ToString() => $"Sequence {Name}";

		public string ToMarkdownInfoString() => "```csharp\n" +
												$"Image \"{ImageName}\"\n" +
		                                        $"Sequence \"{Name}\"\n" +
		                                        $"File name \"{FileName}\"\n" +
		                                        "```";
	}
}
