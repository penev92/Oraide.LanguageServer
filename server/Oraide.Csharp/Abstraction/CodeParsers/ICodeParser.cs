namespace Oraide.Csharp.Abstraction.CodeParsers
{
	public interface ICodeParser
	{
		bool CanParse(in string oraFolderPath);

		CodeInformation Parse(in string oraFolderPath);
	}
}
