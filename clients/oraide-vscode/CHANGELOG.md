# Change Log

All notable changes to the "oraide" extension will be documented in this file.

## 1.0.4 - 2023.09.18

 - Language server:
    - Added support for more modern C# expressions (for loading code symbols).
    - Fixed an exception when changing files from a "mod without symbols".
    - Changed an obscure error message when code symbols can't be loaded.

## 1.0.3 - 2023.03.21

 - Language server:
    - Fixed trait removal hover text.
    - Fixed parsing of boolean class fields always showing default value "false".

## 1.0.2 - 2023.03.21

 - Language server:
    - Fixed exceptions that would sometimes happen when hovering over comments.
    - Fixed parsing of custom attributes' values when they don't use nameof().

## 1.0.1 - 2023.03.18

 - Language server:
    - Fixed a crash when there are multiple asset loader classes with the same name.

## 1.0.0 - 2023.03.17

 - OpenRA Mod support:
    - Added partial support for mod.yaml files.
       - Added LSP features support for referenced files lists.
       - Added LSP features support for asset loaders (Sprite, Sound, Video).
    - Added partial support for chrome layout (UI/widget) files.
       - Added LSP features support for Widgets and their fields.
       - Added LSP features support for ChromeLogic types.
 - Language server:
    - Added proper support for different OpenRA versions.
    - Reworked how code symbol and YAML symbol parsing/generation works.
    - Reworked how LSP requests are handled.
    - Fixed some preexisting bugs.
    - Removed excessive logging to the editor's Output console.

## 0.7.4 - 2023.01.19

 - Language server:
    - Added a band-aid to enable working with installed OpenRA versions even if some symbols can't be loaded.
    - Added support for OpenRA versions release-20210321 and playtest-20230110.

## 0.7.3 - 2022.11.19

 - Language server:
    - Added a hack to trait parsing from code to enable support for old engine versions.
    - Added support for a new VariableDeclarationSyntax to trait parsing to match recent OpenRA engine changes.

## 0.7.2 - 2022.10.31

 - Language server:
    - Fixed hover tooltip showing for WDist fields only in the `0c0` format.
    - Updated trait/projectile/warhead hover tooltip links to the documentation website to reflect its recent changes.

## 0.7.1 - 2022.10.24

 - Language server:
    - Fixed loading of sprite sequence code symbols throwing on older OpenRA codebase versions.

## 0.7.0 - 2022.10.24

 - Language server:
    - Added support for enum-type field values.
    - Added support for boolean field values.
    - Fixed sprite image field detection.

## 0.6.0 - 2022.10.23

 - OpenRA Mod support:
    - Added support for mod (and map) SpriteSequence definitions.
    - Added missed cases to the Palette support.
 - Language server:
    - Added Hover info on node removal.
    - Fixed comments causing exceptions.
    - Improved parsing of class/field/property descriptions from code.

## 0.5.5 - 2022.09.25

 - VSCode integration:
    - Fixed the MiniYAML language definition not defining a comment symbol.

## 0.5.4 - 2022.08.29

 - Language server:
    - Fixed more issues related to internal file/folder path handling in the server.

## 0.5.1 - 0.5.3 - 2022.08.22

 - VSCode integration:
    - Fixed several issues related to language server downloading and storage path.
    - Removed Output window opening on startup.
 - Language server:
    - Fixed a crash when loading empty mod palette definitions.
    - Reworked internal handling of mod file paths.

## 0.5.0 - 2022.07.07

 - OpenRA Mod support:
    - Added support for mod (and map) Palette definitions.
    - Improved handling of Color values in MiniYAML files (now more cases are handled).
    - Fixed not handling rules files from maps inside the SupportDirectory.
 - VSCode integration:
    - Updated [the MiniYAML.tmLanguage](https://github.com/OpenRA/MiniYAML.tmbundle) with further syntax highlighting bug fixes and improvements.
 - Language server:
    - Improved debug logging.

## 0.4.0 - 2022.04.27

 - OpenRA Mod support:
    - Added support for working with OpenRA install directories via static docs files (previously could only parse C# code to generate symbols).
    - Fixed abstract traits/weapons showing up as code completion suggestions.
 - VSCode integration:
    - Fixed a bug that would cause the VSCode extension to hang indefinitely on startup if it couldn't access https://api.github.com/ to check for updates.
    - Updated [the MiniYAML.tmLanguage](https://github.com/OpenRA/MiniYAML.tmbundle) with some syntax highlighting bug fixes and improvements.

## 0.3.1 - 2022.03.29

 - VSCode integration:
    - Allowed the extension to work (for the sake of syntax highlighting) even if the language server can't be started.

## 0.3.0 - 2022.03.27

 - OpenRA Mod support:
    - Added extra hover info for abstract Actor and Weapon definitions.
    - Added hover info for actor names in `map.yaml` files.
    - Added actor `Tooltip.Name` text as navigatable workspace symbols (open with `Ctrl+T`/`Cmd+T`).
 - VSCode integration:
    - Fixed color picker/preview not appearing on lines with multiple color values.
    - Improved syntax highlighting thanks to improvements to [the MiniYAML.tmLanguage](https://github.com/OpenRA/MiniYAML.tmbundle) file.

## 0.2.0 - 2022.03.21

 - OpenRA Mod support:
    - Added proper mod support - loading of `mod.yaml` and referenced rules/weapons/etc. files.
    - Added proper map support - loading of `map.yaml` and referenced rules/weapons/etc. files.
    - Added support for `map.yaml` files (GoTo, AutoComplete, hover information) for relevant supported featuers (actor definitions, referenced files).
 - VSCode integration:
    - Added support for the VSCode color picker and in-line color visualization.
    - Fixed a low-level issue with detecting duplicate strings in a line.

## 0.1.4 - 2022.03.14

 - OpenRA Mod support:
    - Added support for Cursors (GoTo, AutoComplete, information).
 - VSCode integration:
    - Added support for [Breadcrumbs](https://code.visualstudio.com/docs/editor/editingevolved#_breadcrumbs), file [Outline view](https://code.visualstudio.com/docs/getstarted/userinterface#_outline-view) and [DocumentSymbols](https://code.visualstudio.com/docs/editor/editingevolved#_go-to-symbol).
    - The VSCode extension will no longer prompt the user when a new server version is available, instead automatically downloading it.

## 0.1.3 - 2022.03.08

 - Added a "custom" language icon for MiniYAML to match the official YAML icon.
 - Fixed weapon parsing crashing on empty weapons.
 - Fixed symbol cache updating crashing due wrongly handled file paths/urls.
 - Switched to OpenRA/MiniYAML.tmbundle for the MiniYAML language.

## 0.1.2 - 2022.02.17

 - Reintroduced MiniYAML as a custom language for OpenRA .yaml files.

## 0.1.1 - 2022.02.04

 - Improved parsing of C# files:
    - Fixed loading of base types.
    - Added loading of inherited fields/properties.
    - Added loading of class field attributes.
 - Fixed a long-standing issue with the `Name` field of traits implementing `TooltipInfoBase`.
 - Fixed a long-standing issue with IntelliSense suggestions now knowing about inherited fields.
 - Changed extension activation event from *"on YAML file"* to *"if there is a `mod.yaml` file in the workspace"*.

## 0.1.0 (server-v0.1.0) - 2022.02.02

- Added support for weapons:
    - Hover information and navigate to Weapons, Projectiles and Warheads in C# code (if available).
    - IntelliSense for Weapon properties, Projectiles, Warheads and their properties.
    - Find Projectile and Warhead references.
- A lot less excessive debug logging in VSCode's Output window.
- Better handling of map files.
- Major refactorings.
- Some bugfixes.

## 0.0.9 (server-v0.0.6)

- Mostly minor refactoring and bug hunting.

## 0.0.8 (server-v0.0.5)

- Added support for `GoToDeclaration` and `GoToImplementations`/`FindAllImplementations` (both redundant to `GoToDefinition`, but added for completeness).
- Added limited support for `GoToReferences`/`FindAllReferences`.
- Added support for getting workspace symbols (for quick navigation).
- Fixed icon background not being transparent.
- Improved IntelliSense suggestions for trait properties.
- Improved hover tooltip Trait and Trait property descriptions.

## 0.0.7 (server-v0.0.4)

 - Fixed language server sometimes not accepting a mod directory because of a missing `icon.png`.
 - Fixed the language server crashing when there are multiple traits with the same name across namespaces.
 - Fixed IntelliSense not working for trait properties when the current trait has a `@` suffix.
 - Resolved `nameof()` usages in Trait and Trait property descriptions when parsing C# files for code symbols.
 - Improved VSCode extension's contributed settings' descriptions.

## 0.0.6 - 2022.01.31

 - Improved extension README and added feature preview gifs.
 - Added extension icon.
 - Changed extension display name from "ORAIDE" to "OpenRA MiniYAML Language Extension (ORAIDE)"

## 0.0.4 and 0.0.5 - 2022.01.26

 - Fixed extension being broken after custom "miniyaml" language removal. 

## 0.0.3 (server-v0.0.3) - 2022.01.23

 - Started beautifying hover tooltips by using Markdown and by fixing parsing of Desc attributes on traits and trait properties.
 - Added links to online trait documentation.
 - Fixed a couple of small issues with the extension.
 - **Removed custom "miniyaml" language handling to not mess with defaults. That broke some things!**

## 0.0.2 - 2022.01.23

First public release.

## 0.0.2 - 2021.11.08

First private release.