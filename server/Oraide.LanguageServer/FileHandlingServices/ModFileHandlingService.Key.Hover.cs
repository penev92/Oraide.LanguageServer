using System.IO;
using LspTypes;
using Oraide.Core;
using Oraide.Core.Entities.MiniYaml;
using Oraide.LanguageServer.Abstractions.FileHandlingServices;

namespace Oraide.LanguageServer.FileHandlingServices
{
	public partial class ModFileHandlingService
	{
		protected override Hover KeyHover(CursorTarget cursorTarget)
		{
			return cursorTarget.TargetNodeIndentation switch
			{
				0 => HandleKeyHoverAt0(cursorTarget),
				1 => HandleKeyHoverAt1(cursorTarget),
				_ => null
			};
		}

		#region Private methods

		Hover HandleKeyHoverAt0(CursorTarget cursorTarget)
		{
			// File reference lists:
			if (cursorTarget.TargetString == "Rules")
				return IHoverService.HoverFromHoverInfo("A list of references to files that contain **actor** definitions.", range);

			if (cursorTarget.TargetString == "Weapons")
				return IHoverService.HoverFromHoverInfo("A list of references to files that contain **weapon** definitions.", range);

			if (cursorTarget.TargetString == "Sequences")
				return IHoverService.HoverFromHoverInfo("A list of references to files that contain **sprite sequence** definitions.", range);

			if (cursorTarget.TargetString == "ModelSequences")
				return IHoverService.HoverFromHoverInfo("A list of references to files that contain **model sequence** definitions.", range);

			if (cursorTarget.TargetString == "Cursors")
				return IHoverService.HoverFromHoverInfo("A list of references to files that contain **cursor** definitions.", range);

			if (cursorTarget.TargetString == "TileSets")
				return IHoverService.HoverFromHoverInfo("A list of references to files that contain **tileset** definitions.", range);

			if (cursorTarget.TargetString == "Chrome")
				return IHoverService.HoverFromHoverInfo("A list of references to files that contain **chrome** definitions.", range);

			if (cursorTarget.TargetString == "ChromeLayout")
				return IHoverService.HoverFromHoverInfo("A list of references to files that contain **chrome layout** (UI) definitions.", range);

			if (cursorTarget.TargetString == "ChromeMetrics")
				return IHoverService.HoverFromHoverInfo("A list of references to files that contain special **chrome metrics**.", range);

			if (cursorTarget.TargetString == "Translations")
				return IHoverService.HoverFromHoverInfo("A list of references to files that contain **translations**.", range);

			if (cursorTarget.TargetString == "Voices")
				return IHoverService.HoverFromHoverInfo("A list of references to files that contain **voice set** definitions.", range);

			if (cursorTarget.TargetString == "Notifications")
				return IHoverService.HoverFromHoverInfo("A list of references to files that contain **audio notification** definitions.", range);

			if (cursorTarget.TargetString == "Music")
				return IHoverService.HoverFromHoverInfo("A list of references to files that contain **music track** definitions.", range);

			if (cursorTarget.TargetString == "Hotkeys")
				return IHoverService.HoverFromHoverInfo("A list of references to files that contain **hotkey** definitions.", range);

			if (cursorTarget.TargetString == "Missions")
				return IHoverService.HoverFromHoverInfo("A list of references to files that contain **campaign** definitions.", range);

			// Loaders:
			if (cursorTarget.TargetString == "PackageFormats")
				return IHoverService.HoverFromHoverInfo("A list of supported **package** file types.", range);

			if (cursorTarget.TargetString == "SoundFormats")
				return IHoverService.HoverFromHoverInfo("A list of supported **sound** file types.", range);

			if (cursorTarget.TargetString == "SpriteFormats")
				return IHoverService.HoverFromHoverInfo("A list of supported **sprite** file types.", range);

			if (cursorTarget.TargetString == "VideoFormats")
				return IHoverService.HoverFromHoverInfo("A list of supported **video** file types.", range);

			if (cursorTarget.TargetString == "TerrainFormat")
				return IHoverService.HoverFromHoverInfo("The type of **terrain** loader to use.", range);

			if (cursorTarget.TargetString == "SpriteSequenceFormat")
				return IHoverService.HoverFromHoverInfo("The type of **sprite sequence** loader to use.", range);

			if (cursorTarget.TargetString == "ModelSequenceFormat")
				return IHoverService.HoverFromHoverInfo("The type of **model** loader to use.", range);

			return null;
		}

		Hover HandleKeyHoverAt1(CursorTarget cursorTarget)
		{
			// Copy of MapFileHandlingService's file reference handling.
			if (cursorTarget.TargetNode.ParentNode.Key is "Rules" or "Weapons" or "Sequences" or "ModelSequences" or "Cursors" or "TileSets"
			    or "Chrome" or "ChromeLayout" or "ChromeMetrics" or "Translations" or "Voices" or "Notifications" or "Music" or "Hotkeys" or "Missions")
			{
				var resolvedFile = OpenRaFolderUtils.ResolveFilePath(cursorTarget.TargetString, symbolCache.KnownMods);
				if (File.Exists(resolvedFile.AbsolutePath))
					return IHoverService.HoverFromHoverInfo($"```csharp\nFile \"{cursorTarget.TargetString}\"\n```", range);
			}

			return null;
		}

		#endregion
	}
}
