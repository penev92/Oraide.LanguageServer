namespace Oraide.Core.Entities.Csharp
{
	public readonly struct TraitPropertyInfo
	{
		public string PropertyName { get; }

		public string PropertyType { get; }

		public string DefaultValue { get; }

		public MemberLocation Location { get; }

		public string Description { get; }

		public string OtherAttribute { get; }

		public TraitPropertyInfo(string propertyName, string propertyType, string defaultValue, MemberLocation location, string description, string otherAttribute)
		{
			PropertyName = propertyName;
			PropertyType = propertyType;
			DefaultValue = defaultValue;
			Location = location;
			Description = description;
			OtherAttribute = otherAttribute;
		}
	}

	public readonly struct TraitInfo
	{
		public string TraitName { get; }

		public string TraitInfoName { get; }

		public string TraitDescription { get; }

		public MemberLocation Location { get; }

		public string[] InheritedTypes { get; }

		public TraitPropertyInfo[] TraitPropertyInfos { get; }

		public TraitInfo(string traitName, string traitInfoName, string traitDescription, MemberLocation location, string[] inheritedTypes, TraitPropertyInfo[] traitPropertyInfos)
		{
			TraitName = traitName;
			TraitInfoName = traitInfoName;
			TraitDescription = traitDescription;
			Location = location;
			InheritedTypes = inheritedTypes;
			TraitPropertyInfos = traitPropertyInfos;
		}
	}
}
