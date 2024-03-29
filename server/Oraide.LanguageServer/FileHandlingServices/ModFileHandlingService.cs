﻿using System.Collections.Generic;
using System.Linq;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.FileHandlingServices;
using Oraide.LanguageServer.Caching;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class ModFileHandlingService : BaseFileHandlingService
	{
		public ModFileHandlingService(SymbolCache symbolCache, OpenFileCache openFileCache)
			: base(symbolCache, openFileCache) { }

		#region Handler method implementations

		protected override IEnumerable<Location> KeyReferences(CursorTarget cursorTarget) => Enumerable.Empty<Location>();

		protected override IEnumerable<Location> ValueReferences(CursorTarget cursorTarget) => Enumerable.Empty<Location>();

		#endregion
	}
}
