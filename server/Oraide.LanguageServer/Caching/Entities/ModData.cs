namespace Oraide.LanguageServer.Caching.Entities
{
	public class ModData
	{
		public string ModId { get; }

		public string ModFolder { get; }

		public ModSymbols ModSymbols { get; }

		public CodeSymbols CodeSymbols { get; }

		public ModData(string modId, string modFolder, ModSymbols modSymbols, CodeSymbols codeSymbols)
		{
			ModId = modId;
			ModFolder = modFolder;
			ModSymbols = modSymbols;
			CodeSymbols = codeSymbols;
		}
	}
}
