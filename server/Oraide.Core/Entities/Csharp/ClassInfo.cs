namespace Oraide.Core.Entities.Csharp
{
	/// <summary>
	/// Represents information about a TraitInfo (the OpenRA abstract class) or an IProjectileInfo/IWarhead/ISpriteSequence implementation.
	/// </summary>
	public readonly struct ClassInfo
	{
		public readonly string Name;

		public readonly string TypeSuffix;

		public readonly string Description;

		public readonly MemberLocation Location;

		public readonly string[] BaseTypes;

		public readonly ClassFieldInfo[] PropertyInfos;

		public readonly bool IsAbstract;

		readonly string nameWithTypeSuffix;

		public ClassInfo(string name, string typeSuffix, string description, MemberLocation location, string[] baseTypes,
			ClassFieldInfo[] propertyInfos, bool isAbstract)
		{
			Name = name;
			TypeSuffix = typeSuffix;
			Description = description;
			Location = location;
			BaseTypes = baseTypes;
			PropertyInfos = propertyInfos;
			IsAbstract = isAbstract;

			nameWithTypeSuffix = $"{Name}{TypeSuffix}";
		}

		public override string ToString() => nameWithTypeSuffix;

		public string NameWithTypeSuffix => nameWithTypeSuffix;

		public string ToMarkdownInfoString() => "```csharp\n" +
												$"class {nameWithTypeSuffix}" +
												$"\n```\n" +
												$"{Description}";
	}
}
