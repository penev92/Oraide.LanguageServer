using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.FileHandlingServices;
using Oraide.LanguageServer.Extensions;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class ChromeLayoutFileHandlingService : BaseFileHandlingService
	{
		protected override IEnumerable<CompletionItem> KeyCompletion(CursorTarget cursorTarget)
		{
			if (cursorTarget.TargetNodeIndentation % 2 == 0)
				return HandleKeyCompletionAtWidget(cursorTarget);

			return HandleKeyCompletionAtProperty(cursorTarget);
		}

		#region Private methods

		IEnumerable<CompletionItem> HandleKeyCompletionAtWidget(CursorTarget cursorTarget)
		{
			return codeSymbols.Widgets.Select(x => x.First().ToCompletionItem("Widget"));
		}

		IEnumerable<CompletionItem> HandleKeyCompletionAtProperty(CursorTarget cursorTarget)
		{
			// Same thing as handling trait fields.
			var widgetName = cursorTarget.TargetNode.ParentNode.Key.Split("@")[0];
			var presentProperties = cursorTarget.TargetNode.ParentNode.ChildNodes.Select(x => x.Key).ToHashSet();

			return codeSymbols.Widgets[widgetName]
				.SelectMany(x => x.PropertyInfos)
				.DistinctBy(y => y.Name)
				.Where(x => !presentProperties.Contains(x.Name))
				.Select(z => z.ToCompletionItem());
		}

		#endregion
	}
}
