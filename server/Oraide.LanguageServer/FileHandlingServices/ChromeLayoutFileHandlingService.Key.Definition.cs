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
		protected override IEnumerable<Location> KeyDefinition(CursorTarget cursorTarget)
		{
			if (cursorTarget.TargetNodeIndentation % 2 == 0)
				return HandleKeyDefinitionAtWidget(cursorTarget);

			return HandleKeyDefinitionAtProperty(cursorTarget);
		}

		#region Private methods

		IEnumerable<Location> HandleKeyDefinitionAtWidget(CursorTarget cursorTarget)
		{
			// Same thing as handling traits.
			var widgetName = cursorTarget.TargetString.Split("@")[0];
			return codeSymbols.Widgets[widgetName]?
				.Select(x => x.Location.ToLspLocation(widgetName.Length));
		}

		IEnumerable<Location> HandleKeyDefinitionAtProperty(CursorTarget cursorTarget)
		{
			// Same thing as handling trait fields.
			var widgetName = cursorTarget.TargetNode.ParentNode.Key.Split("@")[0];
			return codeSymbols.Widgets[widgetName]?
				.SelectMany(x => x.PropertyInfos.Where(y => y.Name == cursorTarget.TargetNode.Key))
				.Select(x => x.Location.ToLspLocation(x.Name.Length));
		}

		#endregion
	}
}
