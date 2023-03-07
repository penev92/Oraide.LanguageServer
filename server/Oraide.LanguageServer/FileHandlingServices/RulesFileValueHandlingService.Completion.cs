using System;
using System.Collections.Generic;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.FileHandlingServices;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class RulesFileValueHandlingService : ICompletionService
	{
		public IEnumerable<CompletionItem> HandleCompletion(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetNodeIndentation switch
			{
				0 => HandleCompletion0(cursorTarget),
				1 => HandleCompletion1(cursorTarget),
				2 => HandleCompletion2(cursorTarget),
				_ => throw new NotImplementedException()
			};
		}

		#region Private methods

		IEnumerable<CompletionItem> HandleCompletion0(CursorTarget cursorTarget)
		{
			throw new NotImplementedException();
		}

		IEnumerable<CompletionItem> HandleCompletion1(CursorTarget cursorTarget)
		{
			throw new NotImplementedException();
		}

		IEnumerable<CompletionItem> HandleCompletion2(CursorTarget cursorTarget)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
