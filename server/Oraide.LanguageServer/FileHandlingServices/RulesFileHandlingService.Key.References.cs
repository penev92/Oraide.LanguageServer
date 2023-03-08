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
				_ => Enumerable.Empty<Location>()
			};
		}

		#region Private methods

		IEnumerable<Location> HandleKeyReferencesAt1(CursorTarget cursorTarget)
		{
			// Find where else the selected trait is used.
			return symbolCache[cursorTarget.ModId].ModSymbols.ActorDefinitions
				.SelectMany(x =>
					x.SelectMany(y => y.Traits.Where(z => z.Name == cursorTarget.TargetString)))
				.Select(x => x.Location.ToLspLocation(cursorTarget.TargetString.Length));
		}

		#endregion
	}
}
