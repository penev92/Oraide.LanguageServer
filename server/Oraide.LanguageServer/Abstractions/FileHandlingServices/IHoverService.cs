using LspTypes;
using Oraide.Core.Entities.MiniYaml;

namespace Oraide.LanguageServer.Abstractions.FileHandlingServices
{
	public interface IHoverService
	{
		Hover HandleHover(CursorTarget cursorTarget);
	}
}
