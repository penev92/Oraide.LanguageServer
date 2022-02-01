namespace Oraide.Core.Entities.Csharp
{
	/// <summary>
	/// Represents information about a TraitInfo (the OpenRA abstract class).
	/// </summary>
	public readonly struct TraitInfo
	{
		public string TraitName { get; }

		public string TraitInfoName { get; }

		public string TraitDescription { get; }

		public MemberLocation Location { get; }

		public string[] InheritedTypes { get; }

		public ClassFieldInfo[] TraitPropertyInfos { get; }

		public TraitInfo(string traitName, string traitInfoName, string traitDescription, MemberLocation location, string[] inheritedTypes, ClassFieldInfo[] traitPropertyInfos)
		{
			TraitName = traitName;
			TraitInfoName = traitInfoName;
			TraitDescription = traitDescription;
			Location = location;
			InheritedTypes = inheritedTypes;
			TraitPropertyInfos = traitPropertyInfos;
		}

		public override string ToString() => TraitName;

		public string ToMarkdownInfoString() => "```csharp\n" +
		                                        $"class {TraitInfoName}" +
		                                        $"\n```\n" +
		                                        $"{TraitDescription}";
	}
}
