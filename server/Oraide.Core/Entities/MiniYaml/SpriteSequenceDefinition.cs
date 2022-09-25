namespace Oraide.Core.Entities.MiniYaml
{
	public readonly struct SpriteSequenceDefinition
	{
		public readonly string Name;

		public readonly string ImageName;

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
