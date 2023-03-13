using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.FileHandlingServices;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class ChromeLayoutFileHandlingService : BaseFileHandlingService
	{
		protected override Hover ValueHover(CursorTarget cursorTarget)
		{
			if (cursorTarget.TargetNodeIndentation % 2 != 0)
				return HandleValueHoverAtProperty(cursorTarget);

			return null;
		}

		#region Private methods

		Hover HandleValueHoverAtProperty(CursorTarget cursorTarget)
		{
			// Same thing as handling trait fields.
			var widgetName = cursorTarget.TargetNode.ParentNode.Key.Split("@")[0];

			// Using .First() is not great but we have no way to differentiate between traits of the same name
			// until the server learns the concept of a mod and loaded assemblies.
			var widget = codeSymbols.Widgets[widgetName].FirstOrDefault();
			if (widget.Name == null)
				return null;

			var fieldInfo = widget.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.Key);
			if (fieldInfo.Name == null)
				return null;

			// Try to check if this is an enum type field.
			var enumInfo = symbolCache[cursorTarget.ModId].CodeSymbols.EnumInfos.FirstOrDefault(x => x.Key == fieldInfo.InternalType);
			if (enumInfo != null)
			{
				var content = $"```csharp\n{enumInfo.Key}.{cursorTarget.TargetString}\n```";
				return IHoverService.HoverFromHoverInfo(content, range);
			}

			return null;
		}

		#endregion
	}
}
