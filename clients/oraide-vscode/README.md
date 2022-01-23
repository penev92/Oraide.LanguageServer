# Visual Studio Code extension for working with OpenRA's MiniYAML.

## Features

Current features include:
 - Navigating to Traits and Trait properties inside C# code (if available).
 - Navigating to actor and weapon definitions inside parsed MiniYAML files.
 - Hover info for Traits, Trait properties, actor and weapon definitions, conditions and more.
 - Context-aware IntelliSense to help with writing MiniYAML.

## Requirements

The language server requires .NET 6 to run.

## Extension Settings

This extension contributes the following settings:

 - `oraide.server.path`: Path to the language server.
 - `oraide.game.path`: A fallback OpenRA directory path.

## Release Notes

### 0.0.2

First public release.

### Known issues

 - TooltipInfoBase inheritance is ignored, so inheriting types won't know about their `Name` property.
 - Currently only parsing code symbols from C# files is supported. Decompiling game binaries and reading trait information from a static file are not yet implemented.

**Enjoy!**
