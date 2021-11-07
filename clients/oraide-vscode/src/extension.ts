'use strict';

// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import { Trace } from 'vscode-jsonrpc';
import * as vscode from 'vscode';
import * as vscodelc from 'vscode-languageclient/node';
import { ChildProcess, spawn } from 'child_process';
import * as lazy_stream from 'lazystream';
import * as fs from 'fs';

const DEFAULT_SERVER_EXE_NAME_UNIX: string = 'Oraide.LanguageServer.dll';
const DEFAULT_SERVER_EXE_NAME_WINDOWS: string = 'Oraide.LanguageServer.dll';

let client: vscodelc.LanguageClient;

// this method is called when your extension is activated
// your extension is activated the very first time the command is executed
export function activate(context: vscode.ExtensionContext) {

    // // Use the console to output diagnostic information (console.log) and errors (console.error)
    // // This line of code will only be executed once when your extension is activated
    // console.log('Congratulations, your extension "oraide" is now active!');

    // const config = vscode.workspace.getConfiguration('yaml');

    // console.log(JSON.stringify(vscode.workspace));
    // console.log(JSON.stringify(vscode.workspace.getConfiguration()));

    // console.log('Still active!');
    // vscode.languages.registerHoverProvider('javascript', {
    // 	provideHover(document, position, token) {
    // 	  return {
    // 		contents: ['Hover Content']
    // 	  };
    // 	}
    //   });

    const config = vscode.workspace.getConfiguration();
    const serverPath = config.get<string>('oraide.server.path');
    const gameFolderPath = config.get<string>('oraide.game.path');

    // This is supposed to check for undefined, null and empty.
    if (!serverPath) {
        vscode.window.showInformationMessage('Invalid path to ORAIDE Language Server! Please configure via extension settings.');
        return;
    }

    if (!gameFolderPath) {
        vscode.window.showInformationMessage('Invalid path to OpenRA default folder! Please configure via extension settings.');
        return;
    }

    if (vscode.workspace.workspaceFolders === undefined || vscode.workspace.workspaceFolders.length === 0) {
        vscode.window.showInformationMessage('Something went wrong. Extension ORAIDE will abort.');
        return;
    }

    // Explicitly not supporting multi-root workspaces for the moment. May change later depending on requirements.
    // https://code.visualstudio.com/docs/editor/multi-root-workspaces
    const workspaceFolderPath = vscode.workspace.workspaceFolders[0].uri.fsPath;

    // TODO: What if the workspace changes during execution??

    // // The command has been defined in the package.json file
    // // Now provide the implementation of the command with registerCommand
    // // The commandId parameter must match the command field in package.json
    // let disposable = vscode.commands.registerCommand('oraide.helloWorld', () => {
    //     // The code you place here will be executed every time your command is executed
    //     // Display a message box to the user
    //     vscode.window.showInformationMessage('Hello World from oraide!');
    // });

    // context.subscriptions.push(disposable);
    start(context, serverPath!, workspaceFolderPath, gameFolderPath!);
}

// this method is called when your extension is deactivated
export function deactivate() {
    if (!client) {
        return undefined;
    }

    console.log('client stopped.');

    return client.stop();
}

function start(context: vscode.ExtensionContext, serverPath: string, workspaceFolderPath: string, defaultOpenRaPath: string) {
    const serverOptions: vscodelc.ServerOptions = async () => spawnServerProcess(serverPath, workspaceFolderPath, defaultOpenRaPath);

    const clientOptions: vscodelc.LanguageClientOptions = {
        // documentSelector: [
        //     {
        //         pattern: '**/*.yaml',
        //     }
        // ],
        // synchronize: {
        //     // Synchronize the setting section 'languageServerExample' to the server
        //     configurationSection: 'ORAIDE_languageServerExample',
        //     fileEvents: vscode.workspace.createFileSystemWatcher('**/*.yaml')
        // },
        // Register the server for 'miniyaml' (.yaml) files
        documentSelector: [
            {
                language: 'miniyaml',
                scheme: 'file',
            },
            {
                language: 'miniyaml',
                scheme: 'untitled',
            },
        ],
        diagnosticCollectionName: 'OpenRA IDE',
        synchronize: {
            // Notify the server about file changes to '.yaml files contained in the workspace
            configurationSection: 'oraide',
            fileEvents: vscode.workspace.createFileSystemWatcher('**/*.yaml')
        },
        // workspaceFolder: this.dir,
        outputChannelName: "OpenRA IDE",
        // revealOutputChannelOn: RevealOutputChannelOn.Info,
    };

    let client = new vscodelc.LanguageClient('oraide', 'OpenRA IDE', serverOptions, clientOptions, true);

    // ??????
    // client.registerProposedFeatures();

    let disposable = client.start();

    // Push the disposable to the context's subscriptions so that the
    // client can be deactivated on extension deactivation
    context.subscriptions.push(disposable);
}

async function spawnServerProcess(serverPath: string, workspaceFolderPath: string, defaultOpenRaPath: string): Promise<ChildProcess> {

    // Path to the server binary (not executable because reasons).
    // Path to the currently open directory/workspace.
    // Path to the default/configured OpenRA folder (extension will let the user specify a path to it in extension settings).
    const command = "dotnet";
    const args = [
        serverPath,
        workspaceFolderPath,
        defaultOpenRaPath
    ];

    const options = {};

    const serverProcess = spawn(command, args, options);

    if (serverProcess && serverProcess.pid) {
        vscode.window.showInformationMessage(`OpenRA IDE server PID: ${serverProcess.pid}`);
    }

    serverProcess.on('error', (err: { code?: string; message: string }) => {
        if (err.code === 'ENOENT') {
            const msg = `Could not spawn oraide process: ${err.message}`;
            console.error(msg);
            vscode.window.showWarningMessage(msg);
        }
    });

    // Format a `Date` to a human-friendly format.
    //
    // Example: 2019-08-27T07-44-03.log
    const fnFmtDateToHumanFriendlyFileName = (date: Date) => {
        const year = date.getFullYear().toString();
        const month = (date.getMonth() + 101).toString().substring(1);
        const day = (date.getDate() + 100).toString().substring(1);
        const hour = date.getHours().toString().padStart(2, '0');
        const minute = date.getMinutes().toString().padStart(2, '0');
        const second = date.getSeconds().toString().padStart(2, '0');

        const dateStr = [year, month, day].join('-');
        const timeStr = [hour, minute, second].join('-');
        return [dateStr, timeStr].join('T');
    };

    const serverStartDateTime = new Date(); // deliberately using local time
    const formattedDateFileName = fnFmtDateToHumanFriendlyFileName(serverStartDateTime);

    // const logPath = `./${formattedDateFileName}.log`;
    // console.info(`Logging to ${logPath}`);

    // const logStream = new lazy_stream.Writable(() => fs.createWriteStream(logPath, { flags: 'w+' }));
    // serverProcess.stdin.pipe(logStream);
    // serverProcess.stdout.pipe(logStream);
    // serverProcess.stderr.pipe(logStream);

    serverProcess.stdin.on('data', function (data) {
        console.log('stdin: ' + data);
    });

    // serverProcess.stdout.setEncoding('utf8');
    // serverProcess.stdout.on('data', function (data) {
    //     console.log('stdout: ' + data);
    // });

    serverProcess.stderr.setEncoding('utf8');
    serverProcess.stderr.on('data', function (data) {
        console.log('stderr: ' + data);
    });

    serverProcess.on('close', function (code) {
        console.log('closing code: ' + code);
    });

    return serverProcess;
}

// function spawnServerProcess(context: vscode.ExtensionContext) {
// 	let serverExe = 'dotnet';

//     // If the extension is launched in debug mode then the debug server options are used
//     // Otherwise the run options are used
//     let serverOptions: vscodelc.ServerOptions = {
//         run: { command: serverExe, args: ["D:\\Work.Personal\\OpenRA\\Oraide\\Oraide.VSCode\\oraide.ts\\LanguageServer\\Oraide.LanguageServer.dll"] },
//         debug: { command: serverExe, args: ["D:\\Work.Personal\\OpenRA\\Oraide\\Oraide.VSCode\\oraide.ts\\LanguageServer\\Oraide.LanguageServer.dll"] }
//     }

//     // Options to control the language client
//     let clientOptions: vscodelc.LanguageClientOptions = {
//         // Register the server for plain text documents
//         documentSelector: [
//             {
//                 pattern: '**/*.csproj',
//             }
//         ],
//         diagnosticCollectionName: 'OpenRA IDE',
//         synchronize: { configurationSection: 'oraide' },
//         // workspaceFolder: workspace.getWorkspaceFolder(uri),
//         outputChannelName: "OpenRA IDE",
//         // revealOutputChannelOn: RevealOutputChannelOn.Info,
//     };

// 	// Create the language client and start the client.
//     const client = new vscodelc.LanguageClient('languageServerExample', 'Language Server Example', serverOptions, clientOptions);
//     client.trace = Trace.Verbose;
//     let disposable = client.start();
//     console.log("client started.");

//     // Push the disposable to the context's subscriptions so that the
//     // client can be deactivated on extension deactivation
//     context.subscriptions.push(disposable);
// }

// async function spawnServerProcess() /*: Promise<ChildPromise>*/ {

//     var fileName = "D:\\Work.Personal\\OpenRA\\Oraide\\Oraide.VSCode\\oraide.ts\\LanguageServer\\Oraide.LanguageServer.exe";

//     const server: vscodelc.Executable =
//     {
//         command: fileName,
//         args: [],
//         options: { shell: true, detached: false }
//     };

//     const serverOptions: vscodelc.ServerOptions = server;

//     let clientOptions: vscodelc.LanguageClientOptions =
//     {
//         // Register the server for plain text documents
//         // documentSelector: [
//         //     {scheme: 'file', language: 'antlr2'},
//         //     {scheme: 'file', language: 'antlr3'},
//         //     {scheme: 'file', language: 'antlr4'},
//         //     {scheme: 'file', language: 'bison'},
//         //     {scheme: 'file', language: 'ebnf'},
//         //     {scheme: 'file', language: 'iso14977'},
//         //     {scheme: 'file', language: 'lbnf'},
//         // ]
//     };

//     client = new vscodelc.LanguageClient('ORA Language Server', serverOptions, clientOptions);
//     client.registerProposedFeatures();

//     console.log('ORA Language Server is now active!');
//     client.start();
// }
