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

		// TODO: If this is a trait removal, go to where the trait was defined on the actor? (Will require inheritance resolution).
		IEnumerable<Location> HandleKeyDefinitionAt1(CursorTarget cursorTarget)
		{
			var traitName = cursorTarget.TargetNode.Key.Split('@')[0];

			// Until the server learns the concept of a mod and its loaded assemblies (but even then namespaces will be a problem!),
			// we must get all traits that match the target trait name.
			return codeSymbols.TraitInfos[traitName]?
				.Select(x => x.Location.ToLspLocation(cursorTarget.TargetString.Length));
		}

		// TODO: This will likely not handle trait property removals properly!
		IEnumerable<Location> HandleKeyDefinitionAt2(CursorTarget cursorTarget)
		{
			var traitName = cursorTarget.TargetNode.ParentNode.Key.Split('@')[0];

			// Until the server learns the concept of a mod and its loaded assemblies (but even then namespaces will be a problem!),
			// we must get all traits that match the target trait name and get all their fields that match the target trait info field name.
			return codeSymbols.TraitInfos[traitName]?
				.SelectMany(x => x.PropertyInfos.Where(y => y.Name == cursorTarget.TargetNode.Key))
				.Select(x => x.Location.ToLspLocation(cursorTarget.TargetString.Length));
		}

		#endregion
	}
}
