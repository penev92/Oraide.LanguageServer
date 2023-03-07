using System;
using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.FileHandlingServices;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class RulesFileKeyHandlingService : IHoverService
	{
		public Hover HandleHover(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetNodeIndentation switch
			{
				0 => HandleHover0(cursorTarget),
				1 => HandleHover1(cursorTarget),
				2 => HandleHover2(cursorTarget),
				_ => throw new NotImplementedException()
			};
		}

		#region Private methods

		Hover HandleHover0(CursorTarget cursorTarget)
		{
			throw new NotImplementedException();
		}

		Hover HandleHover1(CursorTarget cursorTarget)
		{
			throw new NotImplementedException();
		}

		Hover HandleHover2(CursorTarget cursorTarget)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
