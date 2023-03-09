using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Extensions;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class RulesFileHandlingService
	{
		protected override IEnumerable<Location> KeyReferences(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetNodeIndentation switch
			{
				0 => Enumerable.Empty<Location>(),
				1 => HandleKeyReferencesAt1(cursorTarget),
				2 => HandleKeyReferencesAt2(cursorTarget),
				_ => Enumerable.Empty<Location>()
			};
		}

		#region Private methods

		IEnumerable<Location> HandleKeyReferencesAt1(CursorTarget cursorTarget)
		{
			// Find where else the selected trait is used.
			return modSymbols.ActorDefinitions
				.SelectMany(x =>
					x.SelectMany(y => y.Traits.Where(z => z.Name == cursorTarget.TargetString)))
				.Select(x => x.Location.ToLspLocation(cursorTarget.TargetString.Length));
		}

		// TODO: Can't implement until ActorTraitDefinition implements its `ActorTraitPropertyDefinition[] Properties` field.
		IEnumerable<Location> HandleKeyReferencesAt2(CursorTarget cursorTarget)
		{
			//// Find where else the selected trait's field is used.
			// return modSymbols.ActorDefinitions
			// 	.SelectMany(x =>
			// 		x.SelectMany(y => y.Traits.Where(z => z.Name == cursorTarget.TargetNode.ParentNode?.Key?.Split('@')[0])))
			// 	.Where(x => x.Properties.Any(y => y.Name == cursorTarget.TargetString))
			// 	.Select(x => x.Location.ToLspLocation(cursorTarget.TargetString.Length));
			return Enumerable.Empty<Location>();
		}

		#endregion
	}
}
