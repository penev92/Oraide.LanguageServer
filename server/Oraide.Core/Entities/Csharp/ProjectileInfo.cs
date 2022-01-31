namespace Oraide.Core.Entities.Csharp
{
	/// <summary>
	/// Represents information about an IProjectileInfo implementation.
	/// </summary>
	public readonly struct SimpleClassInfo
	{
		public string Name { get; }

		public string InfoName { get; }

		public string Description { get; }

		public MemberLocation Location { get; }

		public string[] InheritedTypes { get; }

		public ClassFieldInfo[] PropertyInfos { get; }

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
	}
}
