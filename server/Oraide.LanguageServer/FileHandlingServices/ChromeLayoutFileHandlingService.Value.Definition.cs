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
		protected override IEnumerable<Location> ValueDefinition(CursorTarget cursorTarget)
		{
			if (cursorTarget.TargetNodeIndentation % 2 != 0)
				return HandleValueDefinitionAtProperty(cursorTarget);

			return Enumerable.Empty<Location>();
		}

		#region Private methods

		IEnumerable<Location> HandleValueDefinitionAtProperty(CursorTarget cursorTarget)
		{
			// Same thing as handling trait fields.
			var widgetName = cursorTarget.TargetNode.ParentNode.Key.Split("@")[0];

			// Using .First() is not great but we have no way to differentiate between traits of the same name
			// until the server learns the concept of a mod and loaded assemblies.
			var widget = codeSymbols.Widgets[widgetName].FirstOrDefault();
			if (widget.Name == null)
				return Enumerable.Empty<Location>();

			var fieldInfo = widget.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.Key);
			if (fieldInfo.Name == null)
				return Enumerable.Empty<Location>();

			// Try to check if this is an enum type field.
			var enumInfo = codeSymbols.EnumInfos.FirstOrDefault(x => x.Key == fieldInfo.InternalType);
			if (enumInfo != null)
				return new[] { enumInfo.First().Location.ToLspLocation(enumInfo.Key.Length) };

			return Enumerable.Empty<Location>();
		}

		#endregion
	}
}
