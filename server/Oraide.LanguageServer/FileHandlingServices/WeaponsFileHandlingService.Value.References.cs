using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Extensions;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class WeaponsFileHandlingService
	{
		protected override IEnumerable<Location> ValueReferences(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetNodeIndentation switch
			{
				0 => Enumerable.Empty<Location>(),
				1 => HandleValueReferencesAt1(cursorTarget),
				_ => Enumerable.Empty<Location>()
			};
		}

		#region Private methods

		IEnumerable<Location> HandleValueReferencesAt1(CursorTarget cursorTarget)
		{
			var targetNodeKey = cursorTarget.TargetNode.Key;
			if (targetNodeKey == "Projectile")
			{
				// Find where else the selected projectile type is used.
				return modSymbols.WeaponDefinitions
					.SelectMany(x => x.Where(y => y.Projectile.Name == cursorTarget.TargetString))
					.Select(x => x.Projectile.Location.ToLspLocation(cursorTarget.TargetString.Length));
			}

			if (targetNodeKey == "Warhead" || targetNodeKey.StartsWith("Warhead@"))
			{
				// Find where else the selected warhead type is used.
				return modSymbols.WeaponDefinitions
					.SelectMany(x => x.SelectMany(y => y.Warheads.Where(z => z.Name == cursorTarget.TargetString)))
					.Select(x => x.Location.ToLspLocation(cursorTarget.TargetString.Length));
			}

			return Enumerable.Empty<Location>();
		}

		#endregion
	}
}
