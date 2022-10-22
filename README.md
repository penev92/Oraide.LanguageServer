## MiniYAML LanguageServer

[OpenRA](https://github.com/OpenRA/OpenRA/)'s **MiniYAML.LanguageServer** is developed to help people working with [OpenRA .yaml files](https://www.openra.net/book/modding/miniyaml/index.html), making modding OpenRA a less daunting task.

The project consists of a server application and a [VSCode extension](https://marketplace.visualstudio.com/items?itemName=openra.oraide-vscode) communicating via the [Language Server Protocol (LSP)](https://docs.microsoft.com/en-us/visualstudio/extensibility/language-server-protocol) to provide the following features inside VS Code:
 - Great MiniYAML editing support, including Syntax Highlighting, IntelliSense, Go to Definition, Find All References, etc.
 - Navigating inside MiniYAML and to C# (if available).
 - Hover information.
 
 ### How it works
The server is written in C# and provides LSP features for MiniYAML, which is now defined as a custom language.

Syntax highlighting for the custom language is provided by our own [MiniYAML TextMate Grammar file](https://github.com/OpenRA/MiniYAML.tmbundle).

Parsing:
 - For [**MiniYAML**](https://www.openra.net/book/modding/miniyaml/index.html) parsing the server currently uses a copy of OpenRA's own internal parser, but the plan is to package that as a NuGet package for simpler use here.
 - For **C#** parsing the server uses [Roslyn's Syntax Trees](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/get-started/syntax-analysis).

 ## Features
 This is a rough list of features and some of them may get dropped as inapplicable.

  - [X] Hover
  - [X] Goto/Peek/Find Definition/Declaration/Implementations
  - [ ] Find All References
  - [X] Auto Completion
  - [X] Document Symbols
  - [X] Workspace Symbols
  - [X] Syntax Highlight
  - [ ] Rename
  - [ ] Diagnostics
  - [ ] Syntax Check
  - [ ] Code Action

## Using the language server

In order to run the language server you need .NET 6.
