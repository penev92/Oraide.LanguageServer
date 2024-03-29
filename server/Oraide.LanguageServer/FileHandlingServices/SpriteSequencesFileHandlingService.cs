﻿using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.FileHandlingServices;
using Oraide.LanguageServer.Caching;
using Oraide.LanguageServer.Extensions;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class SpriteSequencesFileHandlingService : BaseFileHandlingService
	{
		IEnumerable<CompletionItem> spriteSequenceImageNames;

		public SpriteSequencesFileHandlingService(SymbolCache symbolCache, OpenFileCache openFileCache)
			: base(symbolCache, openFileCache) { }

		public override void Initialize(CursorTarget cursorTarget)
		{
			base.Initialize(cursorTarget);

			spriteSequenceImageNames = modSymbols.SpriteSequenceImageDefinitions.Select(x => x.First().ToCompletionItem());
		}

		#region Handler method implementations

		// TODO: Not implemented yet.
		protected override IEnumerable<Location> KeyReferences(CursorTarget cursorTarget) => Enumerable.Empty<Location>();

		// TODO: Not implemented yet.
		protected override IEnumerable<Location> ValueReferences(CursorTarget cursorTarget) => Enumerable.Empty<Location>();

		#endregion
	}
}
