﻿namespace Oraide.Core.Entities.Csharp
{
	/// <summary>
	/// Represents information about a C# class field.
	/// </summary>
	public readonly struct ClassFieldInfo
	{
		public string PropertyName { get; }

		public string PropertyType { get; }

		public string DefaultValue { get; }

		public MemberLocation Location { get; }

		public string Description { get; }

		public string OtherAttribute { get; }

		public ClassFieldInfo(string propertyName, string propertyType, string defaultValue, MemberLocation location, string description, string otherAttribute)
		{
			PropertyName = propertyName;
			PropertyType = propertyType;
			DefaultValue = defaultValue;
			Location = location;
			Description = description;
			OtherAttribute = otherAttribute;
		}

		public override string ToString() => $"{PropertyType} {PropertyName}";

		public string ToMarkdownInfoString() => "```csharp\n" +
		                                $"{PropertyName} ({PropertyType})" +
		                                $"\n```\n" +
		                                $"{Description}\n\nDefault value: {DefaultValue}";
	}
}