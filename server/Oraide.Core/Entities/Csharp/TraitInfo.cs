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

		public readonly string[] BaseTypes;

		public readonly ClassFieldInfo[] TraitPropertyInfos;

		public readonly bool IsAbstract;

		public TraitInfo(string traitName, string traitInfoName, string traitDescription, MemberLocation location,
			string[] baseTypes, ClassFieldInfo[] traitPropertyInfos, bool isAbstract)
		{
			TraitName = traitName;
			TraitInfoName = traitInfoName;
			TraitDescription = traitDescription;
			Location = location;
			BaseTypes = baseTypes;
			TraitPropertyInfos = traitPropertyInfos;
			IsAbstract = isAbstract;
		}

		public override string ToString() => TraitName;

		public string ToMarkdownInfoString() => "```csharp\n" +
		                                        $"class {TraitInfoName}" +
		                                        $"\n```\n" +
		                                        $"{TraitDescription}";
	}
}
