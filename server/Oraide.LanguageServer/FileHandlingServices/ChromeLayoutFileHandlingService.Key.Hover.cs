using System.Linq;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.FileHandlingServices;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class ChromeLayoutFileHandlingService : BaseFileHandlingService
	{
		protected override Hover KeyHover(CursorTarget cursorTarget)
		{
			if (cursorTarget.TargetNodeIndentation % 2 == 0)
				return HandleKeyHoverAtWidget(cursorTarget);

			return HandleKeyHoverAtProperty(cursorTarget);
		}

		#region Private methods

		Hover HandleKeyHoverAtWidget(CursorTarget cursorTarget)
		{
			var widgetName = cursorTarget.TargetString.Split("@")[0];
			if (codeSymbols.Widgets.Contains(widgetName))
				return IHoverService.HoverFromHoverInfo($"Widget **{widgetName}Widget**.", range);

			return null;
		}

		Hover HandleKeyHoverAtProperty(CursorTarget cursorTarget)
		{
			// Same thing as handling trait fields.
			var widgetName = cursorTarget.TargetNode.ParentNode.Key.Split("@")[0];
			if (codeSymbols.Widgets.Contains(widgetName))
			{
				var widget = codeSymbols.Widgets[widgetName].First();
				var fieldInfo = widget.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetString);
				var content = fieldInfo.ToMarkdownInfoString();
				return IHoverService.HoverFromHoverInfo(content, range);
			}

			return null;
		}

		#endregion
	}
}
