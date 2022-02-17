## MiniYAML LanguageServer

OpenRA's **MiniYAML.LanguageServer** is developed to help people working with OpenRA .yaml files, making modding OpenRA a less daunting task.

The project consists of a server application and a [VSCode extension](https://marketplace.visualstudio.com/items?itemName=openra.oraide-vscode) communicating via the [Language Server Protocol (LSP)](https://docs.microsoft.com/en-us/visualstudio/extensibility/language-server-protocol) to provide the following features inside VS Code:
 - Great MiniYAML editing support, including Syntax Highlighting, IntelliSense, Go to Definition, Find All References, etc.
 - Navigating inside MiniYAML and to C# (if available).
 - Hover information.
 
The server is written in C# and provides LSP features for MiniYAML, which is now defined as a custom language.
 ## Features
 This is a rough list of features and some of them may get dropped as inapplicable.

  - [X] Hover
  - [X] Goto Definition
  - [ ] Find All References
  - [X] Auto Completion
  - [X] IntelliSense
  - [ ] Rename
  - [ ] Document Symbols
  - [X] Workspace Symbols
  - [X] Syntax Highlight
  - [ ] Diagnostics
  - [ ] Syntax Check
  - [ ] Code Action

## Using the language server

In order to run the language server you need .NET 6.
