namespace Oraide.Core.Entities.Csharp
{
	/// <summary>
	/// Represents information about a C# class field.
	/// </summary>
	public readonly struct ClassFieldInfo
	{
		public readonly string Name;

		public readonly string Type;

		public readonly string DefaultValue;

		public readonly MemberLocation Location;

		public readonly string Description;

		public readonly (string Name, string Value)[] OtherAttributes;

		public ClassFieldInfo(string name, string type, string defaultValue, MemberLocation location, string description, (string Name, string Value)[] otherAttributes)
		{
			Name = name;
			Type = type;
			DefaultValue = defaultValue;
			Location = location;
			Description = description;
			OtherAttributes = otherAttributes;
		}

		public override string ToString() => $"{Type} {Name}";

		public string ToMarkdownInfoString() => "```csharp\n" +
		                                $"{Name} ({Type})" +
		                                $"\n```\n" +
		                                $"{Description}\n\nDefault value: {DefaultValue}";
	}
}
