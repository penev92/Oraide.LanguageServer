﻿using System.Collections.Generic;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;

namespace Oraide.LanguageServer.Abstractions.FileHandlingServices
{
	public interface IDefinitionService
	{
		IEnumerable<Location> HandleDefinition(CursorTarget cursorTarget);
	}
}
