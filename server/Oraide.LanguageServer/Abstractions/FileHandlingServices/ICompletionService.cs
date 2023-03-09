using System.Collections.Generic;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;

namespace Oraide.LanguageServer.Abstractions.FileHandlingServices
{
	public interface ICompletionService
	{
		IEnumerable<CompletionItem> HandleCompletion(CursorTarget cursorTarget);
	}
}
