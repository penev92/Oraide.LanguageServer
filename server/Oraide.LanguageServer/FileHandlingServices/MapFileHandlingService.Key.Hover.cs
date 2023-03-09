using LspTypes;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.FileHandlingServices;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class MapFileHandlingService
	{
		protected override Hover KeyHover(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetNodeIndentation switch
			{
				0 => null,
				1 => HandleKeyHoverAt1(cursorTarget),
				2 => HandleKeyHoverAt2(cursorTarget),
				_ => null
			};
		}

		#region Private methods

		Hover HandleKeyHoverAt1(CursorTarget cursorTarget)
		{
			if (cursorTarget.TargetString == "PlayerReference" && cursorTarget.TargetNode?.ParentNode?.Key == "Players")
			{
				// TODO: This could only be useful if documentation for PlayerReference is added.
			}

			if (cursorTarget.TargetNode?.ParentNode?.Key == "Actors")
				return IHoverService.HoverFromHoverInfo("**Actor name**\n\nThis name will be used by potential map scripts and will also show up in the map editor.", range);

			return null;
		}

		Hover HandleKeyHoverAt2(CursorTarget cursorTarget)
		{
			if (cursorTarget.TargetNode?.ParentNode?.Key != null && cursorTarget.TargetNode.ParentNode.Key.StartsWith("PlayerReference"))
			{
				// TODO: This could only be useful if documentation for PlayerReference is added.
			}

			if (cursorTarget.TargetNode?.ParentNode?.ParentNode?.Key == "Actors")
			{
				// TODO: Add support for ActorInits one day.
			}

			return null;
		}

		#endregion
	}
}
