namespace Oraide.Core.Entities.Csharp
{
	/// <summary>
	/// Represents information about a TraitInfo (the OpenRA abstract class) or an IProjectileInfo/IWarhead/ISpriteSequence implementation.
	/// </summary>
	public readonly struct ClassInfo
	{
		public readonly string Name;

		public readonly string InfoName;

		public readonly string Description;

		public readonly MemberLocation Location;

		public readonly string[] BaseTypes;

		public readonly ClassFieldInfo[] PropertyInfos;

		public readonly bool IsAbstract;

		public ClassInfo(string name, string infoName, string description, MemberLocation location, string[] baseTypes,
			ClassFieldInfo[] propertyInfos, bool isAbstract)
		{
			Name = name;
			InfoName = infoName;
			Description = description;
			Location = location;
			BaseTypes = baseTypes;
			PropertyInfos = propertyInfos;
			IsAbstract = isAbstract;
		}

		public override string ToString() => InfoName;

		public string ToMarkdownInfoString() => "```csharp\n" +
												$"class {InfoName}" +
												$"\n```\n" +
												$"{Description}";
	}
}
