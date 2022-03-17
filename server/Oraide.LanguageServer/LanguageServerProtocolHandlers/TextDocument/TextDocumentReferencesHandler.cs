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
	public class TextDocumentReferencesHandler : BaseRpcMessageHandler
	{
		public TextDocumentReferencesHandler(SymbolCache symbolCache, OpenFileCache openFileCache)
			: base(symbolCache, openFileCache) { }

		[OraideCustomJsonRpcMethodTag(Methods.TextDocumentReferencesName)]
		public IEnumerable<Location> References(TextDocumentPositionParams positionParams)
		{
			lock (LockObject)
			{
				try
				{
					if (trace)
						Console.Error.WriteLine("<-- TextDocument-References");

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

		protected override IEnumerable<Location> HandleRulesKey(CursorTarget cursorTarget)
		{
			if (cursorTarget.TargetNodeIndentation == 1)
			{
				// Find where else the selected trait is used.
				return symbolCache[cursorTarget.ModId].ModSymbols.ActorDefinitions
					.SelectMany(x =>
						x.SelectMany(y => y.Traits.Where(z => z.Name == cursorTarget.TargetString)))
					.Select(x => x.Location.ToLspLocation(cursorTarget.TargetString.Length));
			}

			return Enumerable.Empty<Location>();
		}

		protected override IEnumerable<Location> HandleRulesValue(CursorTarget cursorTarget)
		{
			return Enumerable.Empty<Location>();
		}

		protected override IEnumerable<Location> HandleWeaponKey(CursorTarget cursorTarget)
		{
			return Enumerable.Empty<Location>();
		}

		protected override IEnumerable<Location> HandleWeaponValue(CursorTarget cursorTarget)
		{
			if (cursorTarget.TargetNodeIndentation == 1)
			{
				var targetNodeKey = cursorTarget.TargetNode.Key;
				if (targetNodeKey == "Projectile")
				{
					// Find where else the selected projectile type is used.
					return symbolCache[cursorTarget.ModId].ModSymbols.WeaponDefinitions
						.SelectMany(x => x.Where(y => y.Projectile.Name == cursorTarget.TargetString))
						.Select(x => x.Projectile.Location.ToLspLocation(cursorTarget.TargetString.Length));
				}

				if (targetNodeKey == "Warhead" || targetNodeKey.StartsWith("Warhead@"))
				{
					// Find where else the selected warhead type is used.
					return symbolCache[cursorTarget.ModId].ModSymbols.WeaponDefinitions
						.SelectMany(x =>
							x.SelectMany(y => y.Warheads.Where(z => z.Name == cursorTarget.TargetString)))
						.Select(x => x.Location.ToLspLocation(cursorTarget.TargetString.Length));
				}
			}

			return Enumerable.Empty<Location>();
		}

		protected override IEnumerable<Location> HandleCursorsValue(CursorTarget cursorTarget)
		{
			// TODO: Return palette information when we have support for palettes.
			return null;
		}

		#endregion
	}
}
