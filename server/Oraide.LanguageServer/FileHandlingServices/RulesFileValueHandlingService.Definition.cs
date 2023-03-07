using System;
using System.Collections.Generic;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.FileHandlingServices;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class RulesFileValueHandlingService : IDefinitionService
	{
		public IEnumerable<Location> HandleDefinition(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetNodeIndentation switch
			{
				0 => HandleDefinition0(cursorTarget),
				1 => HandleDefinition1(cursorTarget),
				2 => HandleDefinition2(cursorTarget),
				_ => throw new NotImplementedException()
			};
		}

		#region Private methods

		IEnumerable<Location> HandleDefinition0(CursorTarget cursorTarget)
		{
			throw new NotImplementedException();
		}

		IEnumerable<Location> HandleDefinition1(CursorTarget cursorTarget)
		{
			throw new NotImplementedException();
		}

		IEnumerable<Location> HandleDefinition2(CursorTarget cursorTarget)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
