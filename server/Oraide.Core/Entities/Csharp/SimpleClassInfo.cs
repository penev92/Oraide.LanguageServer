namespace Oraide.Core.Entities.Csharp
{
	/// <summary>
	/// Represents information about an IProjectileInfo implementation.
	/// </summary>
	public readonly struct SimpleClassInfo
	{
		public readonly string Name;

		public readonly string InfoName;

		public readonly string Description;

		public readonly MemberLocation Location;

		public readonly string[] InheritedTypes;

		public readonly ClassFieldInfo[] PropertyInfos;

		public SimpleClassInfo(string name, string infoName, string description, MemberLocation location, string[] inheritedTypes, ClassFieldInfo[] propertyInfos)
		{
			Name = name;
			InfoName = infoName;
			Description = description;
			Location = location;
			InheritedTypes = inheritedTypes;
			PropertyInfos = propertyInfos;
		}

		public override string ToString() => InfoName;

		public string ToMarkdownInfoString() => "```csharp\n" +
		                                        $"class {InfoName}" +
		                                        $"\n```\n" +
		                                        $"{Description}";
	}
}
