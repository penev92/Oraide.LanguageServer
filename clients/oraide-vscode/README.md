# Visual Studio Code extension for working with OpenRA's MiniYAML.

## Features

Current features include:
 - Hover info for Traits, Trait properties, actor and weapon definitions, conditions and more.
 - Navigating to Traits, Trait properties, Projectiles, Warheads and more inside C# code (if available).
 - Navigating to actor and weapon definitions inside parsed MiniYAML files.
 - Finding usages/references of Traits, Projectiles and Warheads inside parsed MiniYAML files.
 - Context-aware IntelliSense to help with writing MiniYAML (both for actor and weapon definitions).

*See the bottom of the document for illustrations.

## Requirements

The language server requires .NET 6 to run.

## Extension Settings

This extension contributes the following settings:

 - `oraide.server.path`: Path to the language server.
 - `oraide.game.path`: A fallback OpenRA directory path.

## Release Notes

### 0.1.4

 - Mod support:
    - Added support for mod Cursors (GoTo, AutoComplete, information).
 - VSCode integration:
    - Added support for [Breadcrumbs](https://code.visualstudio.com/docs/editor/editingevolved#_breadcrumbs), file [Outline view](https://code.visualstudio.com/docs/getstarted/userinterface#_outline-view) and [DocumentSymbols](https://code.visualstudio.com/docs/editor/editingevolved#_go-to-symbol).
    - The VSCode extension will no longer prompt the user when a new server version is available, instead automatically downloading it.

### 0.1.3

 - Added a "custom" language icon for MiniYAML to match the official YAML icon.
 - Fixed weapon parsing crashing on empty weapons.
 - Fixed symbol cache updating crashing due wrongly handled file paths/urls.
 - Switched to OpenRA/MiniYAML.tmbundle for the MiniYAML language.

### 0.1.2

 - Reintroduced MiniYAML as a custom language for OpenRA .yaml files.

### 0.1.1

 - Improved parsing of C# files:
    - Fixed loading of base types.
    - Added loading of inherited fields/properties.
    - Added loading of class field attributes.
 - Fixed a long-standing issue with the `Name` field of traits implementing `TooltipInfoBase`.
 - Fixed a long-standing issue with IntelliSense suggestions now knowing about inherited fields.
 - Changed extension activation event from *"on YAML file"* to *"if there is a `mod.yaml` file in the workspace"*.

### 0.1.0 (server-v0.1.0)

- Added support for weapons:
    - Hover information and navigate to Weapons, Projectiles and Warheads in C# code (if available).
    - IntelliSense for Weapon properties, Projectiles, Warheads and their properties.
    - Find Projectile and Warhead references.
- A lot less excessive debug logging in VSCode's Output window.
- Better handling of map files.
- Major refactorings.
- Some bugfixes.

### 0.0.9 (server-v0.0.6)

- Mostly minor refactoring and bug hunting.

### 0.0.8 (server-v0.0.5)

- Added support for `GoToDeclaration` and `GoToImplementations`/`FindAllImplementations` (both redundant to `GoToDefinition`, but added for completeness).
- Added limited support for `GoToReferences`/`FindAllReferences`.
- Added support for getting workspace symbols (for quick navigation).
- Fixed icon background not being transparent.
- Improved IntelliSense suggestions for trait properties.
- Improved hover tooltip Trait and Trait property descriptions.

### 0.0.7 (server-v0.0.4)

 - Fixed language server sometimes not accepting a mod directory because of a missing `icon.png`.
 - Fixed the language server crashing when there are multiple traits with the same name across namespaces.
 - Fixed IntelliSense not working for trait properties when the current trait has a `@` suffix.
 - Resolved `nameof()` usages in Trait and Trait property descriptions when parsing C# files for code symbols.
 - Improved VSCode extension's contributed settings' descriptions.

### 0.0.6

 - Improved extension README and added feature preview gifs.
 - Added extension icon.
 - Changed extension display name from "ORAIDE" to "OpenRA MiniYAML Language Extension (ORAIDE)"

### 0.0.4 and 0.0.5

 - Fixed extension being broken after custom "miniyaml" language removal. 

### 0.0.3 (server-v0.0.3)

 - Started beautifying hover tooltips by using Markdown and by fixing parsing of Desc attributes on traits and trait properties.
 - Added links to online trait documentation.
 - Fixed a couple of small issues with the extension.
 - **Removed custom "miniyaml" language handling to not mess with defaults. That broke some things!**

### 0.0.2

First public release.

### Known issues

 - Currently only parsing code symbols from C# files is supported. Decompiling game binaries and reading trait information from a static file are not yet implemented.
 - The hover tooltip links to the online documentation may or may not work depending on your local version of OpenRA, as the links are never checked for validity and they always target the current release's documentation.
 - Neither hovering/navigation nor autocomplete work very well for traits that match other traits' names from other namespaces.
 - Support for GoToReferences/FindAllReferences currently only works for traits.

**Enjoy!**

## Feature previews:

 - Hovering over text in YAML files will (often) give helpful information:
    <p align="center">
        <img src="https://raw.githubusercontent.com/penev92/Oraide.LanguageServer/main/clients/oraide-vscode/images/docs/Hover_tooltips.gif" alt="" />
    </p>

 - Pressing Ctrl while hovering over text will activate VSCode's PeekDefinition:
    <p align="center">
        <img src="https://raw.githubusercontent.com/penev92/Oraide.LanguageServer/main/clients/oraide-vscode/images/docs/Peek_definition.gif" alt="" />
    </p>

 - Clicking on text will activate VSCode's GoToDefinition:
    - For actor definitions:
        <p align="center">
            <img src="https://raw.githubusercontent.com/penev92/Oraide.LanguageServer/main/clients/oraide-vscode/images/docs/GoToDefinition_1.gif" alt="" />
        </p>

    - For weapon definitions:
        <p align="center">
            <img src="https://raw.githubusercontent.com/penev92/Oraide.LanguageServer/main/clients/oraide-vscode/images/docs/GoToDefinition_2.gif" alt="" />
        </p>

    - For traits definitions:
        <p align="center">
            <img src="https://raw.githubusercontent.com/penev92/Oraide.LanguageServer/main/clients/oraide-vscode/images/docs/GoToDefinition_3.gif" alt="" />
        </p>

    - And more...

 - There is even support for VSCode's IntelliSense:
        <p align="center">
            <img src="https://raw.githubusercontent.com/penev92/Oraide.LanguageServer/main/clients/oraide-vscode/images/docs/IntelliSense.gif" alt="" />
        </p>
        *(activated with Ctrl+Space or whatever your VSCode hotkeys are)*