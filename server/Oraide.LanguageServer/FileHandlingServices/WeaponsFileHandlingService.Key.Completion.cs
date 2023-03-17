using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Extensions;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class WeaponsFileHandlingService
	{
		protected override IEnumerable<CompletionItem> KeyCompletion(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetNodeIndentation switch
			{
				0 => HandleKeyCompletionAt0(cursorTarget),
				1 => HandleKeyCompletionAt1(cursorTarget),
				2 => HandleKeyCompletionAt2(cursorTarget),
				_ => Enumerable.Empty<CompletionItem>()
			};
		}

		#region Private methods

		IEnumerable<CompletionItem> HandleKeyCompletionAt0(CursorTarget cursorTarget)
		{
			// Get only weapon definitions. Presumably for reference and for overriding purposes.
			return weaponNames;
		}

		IEnumerable<CompletionItem> HandleKeyCompletionAt1(CursorTarget cursorTarget)
		{
			// Get only WeaponInfo fields (and "Warhead" and "Inherits").
			return codeSymbols.WeaponInfo.WeaponPropertyInfos
				.Select(x => x.ToCompletionItem())
				.Append(CompletionItems.Warhead)
				.Append(CompletionItems.Inherits);
		}

		IEnumerable<CompletionItem> HandleKeyCompletionAt2(CursorTarget cursorTarget)
		{
			var parentNode = cursorTarget.TargetNode.ParentNode;
			if (parentNode.Key == "Projectile")
			{
				var projectile = codeSymbols.WeaponInfo.ProjectileInfos.FirstOrDefault(x => x.Name == parentNode.Value);
				if (projectile.Name != null)
					return projectile.PropertyInfos.Select(x => x.ToCompletionItem());
			}
			else if (parentNode.Key == "Warhead" || parentNode.Key.StartsWith("Warhead@"))
			{
				var warhead = codeSymbols.WeaponInfo.WarheadInfos.FirstOrDefault(x => x.Name == parentNode.Value);
				if (warhead.Name != null)
					return warhead.PropertyInfos.Select(x => x.ToCompletionItem());
			}

			return Enumerable.Empty<CompletionItem>();
		}

		#endregion
	}
}
