namespace Oraide.Core.Entities.Csharp
{
	public readonly struct TraitPropertyInfo
	{
		public string PropertyName { get; }

		public MemberLocation Location { get; }

		public TraitPropertyInfo(string propertyName, MemberLocation location)
		{
			PropertyName = propertyName;
			Location = location;
		}
	}

	public readonly struct TraitInfo
	{
		public string TraitName { get; }

		public string TraitDescription { get; }

		public MemberLocation Location { get; }

		public string[] InheritedTypes { get; }

		public TraitPropertyInfo[] TraitPropertyInfos { get; }

		public TraitInfo(string traitName, string traitDescription, MemberLocation location, string[] inheritedTypes, TraitPropertyInfo[] traitPropertyInfos)
		{
			TraitName = traitName;
			TraitDescription = traitDescription;
			Location = location;
			InheritedTypes = inheritedTypes;
			TraitPropertyInfos = traitPropertyInfos;
		}
	}
}
