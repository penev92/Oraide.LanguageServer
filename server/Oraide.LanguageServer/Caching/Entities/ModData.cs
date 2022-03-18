using Oraide.Core.Entities.MiniYaml;

namespace Oraide.LanguageServer.Caching.Entities
{
	public class ModData
	{
		public string ModId { get; }

		public string ModFolder { get; }

		public ModManifest ModManifest { get; }

		public ModSymbols ModSymbols { get; }

		public CodeSymbols CodeSymbols { get; }

		public MapManifest[] Maps { get; }

		public ModData(string modId, string modFolder, ModManifest modManifest, ModSymbols modSymbols, CodeSymbols codeSymbols, MapManifest[] maps)
		{
			ModId = modId;
			ModFolder = modFolder;
			ModManifest = modManifest;
			ModSymbols = modSymbols;
			CodeSymbols = codeSymbols;
			Maps = maps;
		}
	}
}
