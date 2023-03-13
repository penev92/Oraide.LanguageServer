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
		protected override IEnumerable<CompletionItem> ValueCompletion(CursorTarget cursorTarget)
		{
			if (cursorTarget.TargetNodeIndentation % 2 != 0)
				return HandleValueCompletionAtProperty(cursorTarget);

			return Enumerable.Empty<CompletionItem>();
		}

		#region Private methods

		IEnumerable<CompletionItem> HandleValueCompletionAtProperty(CursorTarget cursorTarget)
		{
			// Same thing as handling trait fields.
			var widgetName = cursorTarget.TargetNode.ParentNode.Key.Split("@")[0];

			// Using .First() is not great but we have no way to differentiate between traits of the same name
			// until the server learns the concept of a mod and loaded assemblies.
			var widget = codeSymbols.Widgets[widgetName].FirstOrDefault();
			if (widget.Name == null)
				return Enumerable.Empty<CompletionItem>();

			var fieldInfo = widget.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.Key);
			if (fieldInfo.Name == null)
				return Enumerable.Empty<CompletionItem>();

			// Try to check if this is an enum type field.
			var enumInfo = codeSymbols.EnumInfos.FirstOrDefault(x => x.Key == fieldInfo.InternalType);
			if (enumInfo != null)
			{
				return enumInfo.FirstOrDefault().Values.Select(x => new CompletionItem
				{
					Label = x,
					Kind = CompletionItemKind.EnumMember,
					Detail = "Enum type value",
					Documentation = $"{enumInfo.Key}.{x}"
				});
			}

			if (fieldInfo.InternalType == "bool")
				return new[] { CompletionItems.True, CompletionItems.False };

			return Enumerable.Empty<CompletionItem>();
		}

		#endregion
	}
}
