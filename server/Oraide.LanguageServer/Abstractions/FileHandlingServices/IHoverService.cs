using LspTypes;
using Oraide.Core.Entities.MiniYaml;

namespace Oraide.LanguageServer.Abstractions.FileHandlingServices
{
	public interface IHoverService
	{
		Hover HandleHover(CursorTarget cursorTarget);

		static Hover HoverFromHoverInfo(string content, Range range)
		{
			return new Hover
			{
				Contents = new SumType<string, MarkedString, MarkedString[], MarkupContent>(new MarkupContent
				{
					Kind = MarkupKind.Markdown,
					Value = content
				}),
				Range = range
			};
		}
	}
}
