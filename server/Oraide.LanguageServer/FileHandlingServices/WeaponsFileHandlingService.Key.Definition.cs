using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Extensions;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class WeaponsFileHandlingService
	{
		protected override IEnumerable<Location> KeyDefinition(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetNodeIndentation switch
			{
				0 => HandleKeyDefinitionAt0(cursorTarget),
				1 => HandleKeyDefinitionAt1(cursorTarget),
				2 => HandleKeyDefinitionAt2(cursorTarget),
				_ => Enumerable.Empty<Location>()
			};
		}

		#region Private methods

		IEnumerable<Location> HandleKeyDefinitionAt0(CursorTarget cursorTarget)
		{
			var weaponDefinitions = modSymbols.WeaponDefinitions.FirstOrDefault(x => x.Key == cursorTarget.TargetString);
			if (weaponDefinitions != null)
				return weaponDefinitions.Select(x => x.Location.ToLspLocation(weaponDefinitions.Key.Length));

			return Enumerable.Empty<Location>();
		}

		IEnumerable<Location> HandleKeyDefinitionAt1(CursorTarget cursorTarget)
		{
			var targetString = cursorTarget.TargetString;
			if (cursorTarget.TargetString == "Warhead")
				targetString = "Warheads"; // Hacks!

			var fieldInfo = codeSymbols.WeaponInfo.WeaponPropertyInfos.FirstOrDefault(x => x.Name == targetString);
			if (fieldInfo.Name != null)
				return new[] { fieldInfo.Location.ToLspLocation(fieldInfo.Name.Length) };

			return Enumerable.Empty<Location>();
		}

		IEnumerable<Location> HandleKeyDefinitionAt2(CursorTarget cursorTarget)
		{
			var parentNodeKey = cursorTarget.TargetNode.ParentNode.Key;
			if (parentNodeKey == "Projectile")
			{
				var projectile = codeSymbols.WeaponInfo.ProjectileInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.ParentNode.Value);
				if (projectile.Name != null)
				{
					var fieldInfo = projectile.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetString);
					if (fieldInfo.Name != null)
						return new[] { fieldInfo.Location.ToLspLocation(fieldInfo.Name.Length) };
				}
			}
			else if (parentNodeKey == "Warhead" || parentNodeKey.StartsWith("Warhead@"))
			{
				var warhead = codeSymbols.WeaponInfo.WarheadInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.ParentNode.Value);
				if (warhead.Name != null)
				{
					var fieldInfo = warhead.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetString);
					if (fieldInfo.Name != null)
						return new[] { fieldInfo.Location.ToLspLocation(fieldInfo.Name.Length) };
				}
			}

			return Enumerable.Empty<Location>();
		}

		#endregion
	}
}
