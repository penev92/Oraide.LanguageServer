namespace Oraide.Core.Entities.Csharp
{
	/// <summary>
	/// Represents information about a TraitInfo (the OpenRA abstract class).
	/// </summary>
	public readonly struct TraitInfo
	{
		public readonly string TraitName;

		public readonly string TraitInfoName;

		public readonly string TraitDescription;

		public readonly MemberLocation Location;

		public readonly string[] InheritedTypes;

		public readonly ClassFieldInfo[] TraitPropertyInfos;

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
