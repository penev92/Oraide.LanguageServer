namespace Oraide.Core.Entities
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
}
