namespace Oraide.LanguageServer.CodeParsers
{
	public readonly struct MemberLocation
	{
		public string FilePath { get; }

		public int LineNumber { get; }

		public int CharacterPosition { get; }

		public MemberLocation(string filePath, int lineNumber, int characterPosition)
		{
			FilePath = filePath;
			LineNumber = lineNumber;
			CharacterPosition = characterPosition;
		}
	}

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

	public enum CodeMemberType
	{
		Class = 1,
		Trait = 2,
		Property = 3
	}
}
