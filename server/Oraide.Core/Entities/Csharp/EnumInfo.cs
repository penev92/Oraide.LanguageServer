namespace Oraide.Core.Entities.Csharp
{
	/// <summary>
	/// Represents information about an enum type.
	/// </summary>
	public readonly struct EnumInfo
	{
		public readonly string Name;

		public readonly string FullName;

		public readonly string Description;

		public readonly string[] Values;

		public readonly bool IsFlagsEnum;

		public readonly MemberLocation Location;

		public EnumInfo(string name, string fullName, string description, string[] values, bool isFlagsEnum, MemberLocation location)
		{
			Name = name;
			FullName = fullName;
			Description = description;
			Values = values;
			IsFlagsEnum = isFlagsEnum;
			Location = location;
		}

		public override string ToString() => FullName;

		public string ToMarkdownInfoString() => "```csharp\n" +
		                                        $"enum {Name}" + (IsFlagsEnum ? " (flags)" : "") +
		                                        $"\n```\n" +
		                                        $"{Description}";
	}
}
