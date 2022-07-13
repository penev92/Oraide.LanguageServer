using System;
using System.IO;

namespace Oraide.Core.Entities
{
	public readonly struct MemberLocation
	{
		public readonly Uri FileUri;

		public readonly int LineNumber;

		public readonly int CharacterPosition;

		public MemberLocation(string fileUriString, int lineNumber, int characterPosition)
		{
			FileUri = File.Exists(fileUriString) || Uri.IsWellFormedUriString(fileUriString, UriKind.Absolute) ? new Uri(fileUriString) : null;
			LineNumber = lineNumber;
			CharacterPosition = characterPosition;
		}

		public MemberLocation(Uri fileUri, int lineNumber, int characterPosition)
		{
			FileUri = fileUri;
			LineNumber = lineNumber;
			CharacterPosition = characterPosition;
		}

		public override string ToString() => $"{FileUri?.AbsolutePath ?? "<empty>"}:{LineNumber},{CharacterPosition}";

		// TODO: Implement equality.
	}
}
