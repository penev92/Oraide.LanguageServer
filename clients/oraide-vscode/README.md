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

### Known issues

 - The hover tooltip links to the online documentation may or may not work depending on your local version of OpenRA, as the links are never checked for validity and they always target the current release's documentation.
 - Neither hovering/navigation nor autocomplete work very well for traits that match other traits' names from other namespaces.
 - Support for GoToReferences/FindAllReferences currently only works for traits.
 - As of 16.08.2022 a VSCode update broke something about Ctrl+hovering over symbols, defined in an unopened file. I currently have no idea what's going on or how to fix it.
 - Hover and GoTo don't work for sequences that are inherited by another image. Only for sequences defined on the referenced image.

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