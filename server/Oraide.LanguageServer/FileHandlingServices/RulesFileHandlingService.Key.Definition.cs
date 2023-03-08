using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Extensions;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class RulesFileHandlingService
	{
		protected override IEnumerable<Location> KeyDefinition(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetNodeIndentation switch
			{
				0 => Enumerable.Empty<Location>(),
				1 => HandleKeyDefinitionAt1(cursorTarget),
				2 => HandleKeyDefinitionAt2(cursorTarget),
				_ => Enumerable.Empty<Location>()
			};
		}

		#region Private methods

		IEnumerable<Location> HandleKeyDefinitionAt1(CursorTarget cursorTarget)
		{
			var traitName = cursorTarget.TargetNode.Key.Split('@')[0];
			var traitInfoName = $"{traitName}Info";

			// Using .First() is not great but we have no way to differentiate between traits of the same name
			// until the server learns the concept of a mod and loaded assemblies.
			var traitInfo = codeSymbols.TraitInfos[traitInfoName].FirstOrDefault();
			if (traitInfo.Name != null)
				return new[] { traitInfo.Location.ToLspLocation(cursorTarget.TargetString.Length) };

			return Enumerable.Empty<Location>();
		}

		IEnumerable<Location> HandleKeyDefinitionAt2(CursorTarget cursorTarget)
		{
			var traitName = cursorTarget.TargetNode.ParentNode.Key.Split('@')[0];
			var traitInfoName = $"{traitName}Info";

			// Using .First() is not great but we have no way to differentiate between traits of the same name
			// until the server learns the concept of a mod and loaded assemblies.
			var traitInfo = codeSymbols.TraitInfos[traitInfoName].FirstOrDefault();
			if (traitInfo.Name != null)
			{
				var fieldInfo = traitInfo.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.Key);
				if (fieldInfo.Name != null)
					return new[] { fieldInfo.Location.ToLspLocation(cursorTarget.TargetString.Length) };
			}

			return Enumerable.Empty<Location>();
		}

		#endregion
	}
}
