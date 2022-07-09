namespace Oraide.Core.Entities
{
	public readonly struct MemberLocation
	{
		public readonly string FilePath;

		public readonly int LineNumber;

		public readonly int CharacterPosition;

		public MemberLocation(string filePath, int lineNumber, int characterPosition)
		{
			FilePath = filePath;
			LineNumber = lineNumber;
			CharacterPosition = characterPosition;
		}

		public override string ToString() => $"{FilePath ?? "<empty>"}:{LineNumber},{CharacterPosition}";
	}
}
