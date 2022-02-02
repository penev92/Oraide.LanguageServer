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

					if (TryGetCursorTarget(positionParams, out var target))
					{
						if (target.FileType == FileType.Rules)
						{
							if (target.TargetType == "key"
							    && (target.TargetNodeIndentation == 1 || target.TargetNodeIndentation == 2)
							    && TryGetTargetCodeDefinitionLocations(target, out var codeDefinitionLocations))
								return codeDefinitionLocations;

							if ((target.TargetType == "value" || target.TargetNodeIndentation == 0)
							    && TryGetTargetYamlDefinitionLocations(target, out var yamlDefinitionLocations))
								return yamlDefinitionLocations;
						}
						else if (target.FileType == FileType.Weapons)
						{
							var weaponInfo = symbolCache[target.ModId].WeaponInfo;

							if (target.TargetNodeIndentation == 0)
							{
								var weaponDefinitions = symbolCache[target.ModId].WeaponDefinitions.FirstOrDefault(x => x.Key == target.TargetString);
								if (weaponDefinitions != null)
									return weaponDefinitions.Select(x => x.Location.ToLspLocation(weaponDefinitions.Key.Length));
							}
							else if (target.TargetNodeIndentation == 1)
							{
								if (target.TargetType == "key")
								{
									var targetString = target.TargetString;
									if (target.TargetString == "Warhead")
										targetString = "Warheads"; // Hacks!

									var fieldInfo = weaponInfo.WeaponPropertyInfos.FirstOrDefault(x => x.Name == targetString);
									if (fieldInfo.Name != null)
										return new[] { fieldInfo.Location.ToLspLocation(fieldInfo.Name.Length) };
								}
								else if (target.TargetType == "value")
								{
									var targetNodeKey = target.TargetNode.Key;
									if (targetNodeKey == "Inherits")
									{
										var weaponDefinitions = symbolCache[target.ModId].WeaponDefinitions.FirstOrDefault(x => x.Key == target.TargetString);
										if (weaponDefinitions != null)
											return weaponDefinitions.Select(x => x.Location.ToLspLocation(weaponDefinitions.Key.Length));
									}
									else if (targetNodeKey == "Projectile")
									{
										var projectile = weaponInfo.ProjectileInfos.FirstOrDefault(x => x.Name == target.TargetString);
										if (projectile.Name != null)
											return new[] { projectile.Location.ToLspLocation(projectile.Name.Length) };
									}
									else if (targetNodeKey == "Warhead" || targetNodeKey.StartsWith("Warhead@"))
									{
										var warhead = weaponInfo.WarheadInfos.FirstOrDefault(x => x.Name == $"{target.TargetString}Warhead");
										if (warhead.Name != null)
											return new[] { warhead.Location.ToLspLocation(warhead.Name.Length) };
									}
								}
							}
							else if (target.TargetNodeIndentation == 2)
							{
								var parentNodeKey = target.TargetNode.ParentNode.Key;
								if (parentNodeKey == "Projectile")
								{
									var projectile = weaponInfo.ProjectileInfos.FirstOrDefault(x => x.Name == target.TargetNode.ParentNode.Value);
									if (projectile.Name != null)
									{
										var fieldInfo = projectile.PropertyInfos.FirstOrDefault(x => x.Name == target.TargetString);
										if (fieldInfo.Name != null)
											return new[] { fieldInfo.Location.ToLspLocation(fieldInfo.Name.Length) };
									}
								}
								else if (parentNodeKey == "Warhead" || parentNodeKey.StartsWith("Warhead@"))
								{
									var warhead = weaponInfo.WarheadInfos.FirstOrDefault(x => x.Name == $"{target.TargetNode.ParentNode.Value}Warhead");
									if (warhead.Name != null)
									{
										// TODO: Check base types for inherited properties.
										var fieldInfo = warhead.PropertyInfos.FirstOrDefault(x => x.Name == target.TargetString);
										if (fieldInfo.Name != null)
											return new[] { fieldInfo.Location.ToLspLocation(fieldInfo.Name.Length) };
									}
								}
							}
						}
					}
				}
				catch (Exception e)
				{
					Console.Error.WriteLine("EXCEPTION!!!");
					Console.Error.WriteLine(e.ToString());
				}

				return Enumerable.Empty<Location>();
			}
		}

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
