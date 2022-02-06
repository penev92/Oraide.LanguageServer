using System;
using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.LanguageServerProtocolHandlers;
using Oraide.LanguageServer.Caching;
using Oraide.LanguageServer.Extensions;

namespace Oraide.LanguageServer.LanguageServerProtocolHandlers.TextDocument
{
	public class TextDocumentDefinitionHandler : BaseRpcMessageHandler
	{
		public TextDocumentDefinitionHandler(SymbolCache symbolCache, OpenFileCache openFileCache)
			: base(symbolCache, openFileCache) { }

		[OraideCustomJsonRpcMethodTag(Methods.TextDocumentDefinitionName)]
		public IEnumerable<Location> Definition(TextDocumentPositionParams positionParams)
		{
			lock (LockObject)
			{
				try
				{
					if (trace)
						Console.Error.WriteLine("<-- TextDocument-Definition");

					return HandlePositionalRequest(positionParams) as IEnumerable<Location>;
				}
				catch (Exception e)
				{
					Console.Error.WriteLine("EXCEPTION!!!");
					Console.Error.WriteLine(e.ToString());
				}

				return Enumerable.Empty<Location>();
			}
		}

		#region CursorTarget handlers

		// TODO:
		protected override IEnumerable<Location> HandleRulesKey(CursorTarget cursorTarget)
		{
			if ((cursorTarget.TargetNodeIndentation == 1 || cursorTarget.TargetNodeIndentation == 2)
			    && TryGetTargetCodeDefinitionLocations(cursorTarget, out var codeDefinitionLocations))
				return codeDefinitionLocations;

			return Enumerable.Empty<Location>();
		}

		// TODO:
		protected override IEnumerable<Location> HandleRulesValue(CursorTarget cursorTarget)
		{
			if ((cursorTarget.TargetType == "value" || cursorTarget.TargetNodeIndentation == 0)
			    && TryGetTargetYamlDefinitionLocations(cursorTarget, out var yamlDefinitionLocations))
				return yamlDefinitionLocations;

			return Enumerable.Empty<Location>();
		}

		protected override IEnumerable<Location> HandleWeaponKey(CursorTarget cursorTarget)
		{
			var weaponInfo = symbolCache[cursorTarget.ModId].WeaponInfo;

			switch (cursorTarget.TargetNodeIndentation)
			{
				case 0:
				{
					var weaponDefinitions = symbolCache[cursorTarget.ModId].WeaponDefinitions.FirstOrDefault(x => x.Key == cursorTarget.TargetString);
					if (weaponDefinitions != null)
						return weaponDefinitions.Select(x => x.Location.ToLspLocation(weaponDefinitions.Key.Length));

					return Enumerable.Empty<Location>();
				}

				case 1:
				{
					var targetString = cursorTarget.TargetString;
					if (cursorTarget.TargetString == "Warhead")
						targetString = "Warheads"; // Hacks!

					var fieldInfo = weaponInfo.WeaponPropertyInfos.FirstOrDefault(x => x.Name == targetString);
					if (fieldInfo.Name != null)
						return new[] { fieldInfo.Location.ToLspLocation(fieldInfo.Name.Length) };

					return Enumerable.Empty<Location>();
				}

				case 2:
				{
					var parentNodeKey = cursorTarget.TargetNode.ParentNode.Key;
					if (parentNodeKey == "Projectile")
					{
						var projectile = weaponInfo.ProjectileInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.ParentNode.Value);
						if (projectile.Name != null)
						{
							var fieldInfo = projectile.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetString);
							if (fieldInfo.Name != null)
								return new[] { fieldInfo.Location.ToLspLocation(fieldInfo.Name.Length) };
						}
					}
					else if (parentNodeKey == "Warhead" || parentNodeKey.StartsWith("Warhead@"))
					{
						var warhead = weaponInfo.WarheadInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetNode.ParentNode.Value);
						if (warhead.Name != null)
						{
							// TODO: Check base types for inherited properties.
							var fieldInfo = warhead.PropertyInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetString);
							if (fieldInfo.Name != null)
								return new[] { fieldInfo.Location.ToLspLocation(fieldInfo.Name.Length) };
						}
					}

					return Enumerable.Empty<Location>();
				}

				default:
					return Enumerable.Empty<Location>();
			}
		}

		protected override IEnumerable<Location> HandleWeaponValue(CursorTarget cursorTarget)
		{
			var weaponInfo = symbolCache[cursorTarget.ModId].WeaponInfo;

			switch (cursorTarget.TargetNodeIndentation)
			{
				case 0:
					return Enumerable.Empty<Location>();

				case 1:
				{
					var targetNodeKey = cursorTarget.TargetNode.Key;
					if (targetNodeKey == "Inherits")
					{
						var weaponDefinitions = symbolCache[cursorTarget.ModId].WeaponDefinitions.FirstOrDefault(x => x.Key == cursorTarget.TargetString);
						if (weaponDefinitions != null)
							return weaponDefinitions.Select(x => x.Location.ToLspLocation(weaponDefinitions.Key.Length));
					}
					else if (targetNodeKey == "Projectile")
					{
						var projectile = weaponInfo.ProjectileInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetString);
						if (projectile.Name != null)
							return new[] { projectile.Location.ToLspLocation(projectile.Name.Length) };
					}
					else if (targetNodeKey == "Warhead" || targetNodeKey.StartsWith("Warhead@"))
					{
						var warhead = weaponInfo.WarheadInfos.FirstOrDefault(x => x.Name == cursorTarget.TargetString);
						if (warhead.Name != null)
							return new[] { warhead.Location.ToLspLocation(warhead.Name.Length) };
					}

					return Enumerable.Empty<Location>();
				}

				case 2:
					return Enumerable.Empty<Location>();

				default:
					return Enumerable.Empty<Location>();
			}
		}

		#endregion

		private bool TryGetTargetCodeDefinitionLocations(CursorTarget target, out IEnumerable<Location> definitionLocations)
		{
			if (!TryGetCodeMemberLocation(target.TargetNode, target.TargetString, out var traitInfo, out var location))
			{
				definitionLocations = default;
				return false;
			}

			definitionLocations = new[] { location.ToLspLocation(target.TargetString.Length) };
			return true;
		}

		private bool TryGetTargetYamlDefinitionLocations(CursorTarget target, out IEnumerable<Location> definitionLocations)
		{
			// Check targetNode node type - probably via IndentationLevel enum.
			// If it is a top-level node *and this is an actor-definition or a weapon-definition file* it definitely is a definition.
			// If it is indented once we need to check if the target is the key or the value - keys are traits, but values *could* reference actor/weapon definitions.

			var targetLength = target.TargetString.Length;

			definitionLocations =
				symbolCache[target.ModId].ActorDefinitions[target.TargetString].Select(x => x.Location.ToLspLocation(targetLength))
				.Union(symbolCache[target.ModId].WeaponDefinitions[target.TargetString].Select(x => x.Location.ToLspLocation(targetLength)))
				.Union(symbolCache[target.ModId].ConditionDefinitions[target.TargetString].Select(x => x.Location.ToLspLocation(targetLength)));

			return true;
		}
	}
}
