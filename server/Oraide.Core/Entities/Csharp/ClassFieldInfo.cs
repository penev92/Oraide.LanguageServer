using System.Linq;

namespace Oraide.Core.Entities.Csharp
{
	/// <summary>
	/// Represents information about a C# class field.
	/// </summary>
	public readonly struct ClassFieldInfo
	{
		public readonly string Name;

		public readonly string InternalType;

		public readonly string UserFriendlyType;

		public readonly string DefaultValue;

		public readonly string ClassName;

		public readonly MemberLocation Location;

		public readonly string Description;

		public readonly (string Name, string Value)[] OtherAttributes;

		public ClassFieldInfo(string name, string internalType, string userFriendlyType, string defaultValue, string className,
			MemberLocation location, string description, (string Name, string Value)[] otherAttributes)
		{
			Name = name;
			InternalType = internalType;
			UserFriendlyType = userFriendlyType;
			DefaultValue = defaultValue;
			ClassName = className;
			Location = location;
			Description = description;
			OtherAttributes = otherAttributes;
		}

		public override string ToString() => $"{InternalType} {Name}";

		public string ToMarkdownInfoString()
		{
			var content = "```csharp\n" +
			              $"{Name}" + (string.IsNullOrWhiteSpace(InternalType) ? "" : $" - {InternalType} ({UserFriendlyType})") +
			              "\n```\n" +
			              $"{Description}";

			if (!string.IsNullOrWhiteSpace(DefaultValue))
				content += $"\n\nDefault value: {DefaultValue}";

			if (OtherAttributes != null && OtherAttributes.Length != 0)
				content += "\n" +
				           "```yaml\n\n" +
						   "Field Attributes\n" +
				           $"{string.Join("\n", OtherAttributes.Select(x => $"  {x.Name}: {x.Value}"))}" +
				           "\n```";

			return content;
		}
	}
}
