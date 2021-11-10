'use strict';

// The module 'vscode' contains the VS Code extensibility API
import * as vscode from 'vscode';
import * as vscodelc from 'vscode-languageclient/node';

import * as utils from './utils';
import * as languageServer from './languageServer';

let client: vscodelc.LanguageClient;

export async function activate(context: vscode.ExtensionContext) {

    const config = vscode.workspace.getConfiguration();
    // const serverPath = config.get<string>('oraide.server.path');
    const gameFolderPath = config.get<string>('oraide.game.path');

    // // This is supposed to check for undefined, null and empty.
    // if (!serverPath) {
    //     vscode.window.showInformationMessage('Invalid path to ORAIDE Language Server! Please configure via extension settings.');
    //     return;
    // }

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

    let serverPath = undefined;
    if (utils.IS_DEBUG) {
        vscode.window.showInformationMessage(`IS DEBUG!!`);
        serverPath = config.get<string>('oraide.server.path');
    } else {
        serverPath = await languageServer.findOrDownloadLanguageServer(context);
        if (!serverPath) {
            vscode.window.showInformationMessage('Extension ORAIDE failed to find or download its language server and will abort.');
            return;
        }
    }

    start(context, serverPath!, workspaceFolderPath, gameFolderPath!);
}

// this method is called when your extension is deactivated
export function deactivate() {
    if (!client) {
        return undefined;
    }

    if (isDebug) {
        console.log('client stopped.');
    }

    return client.stop();
}

function start(context: vscode.ExtensionContext, serverPath: string, workspaceFolderPath: string, defaultOpenRaPath: string) {
    const serverOptions: vscodelc.ServerOptions = async () => languageServer.spawnServerProcess(serverPath, workspaceFolderPath, defaultOpenRaPath);

    const clientOptions: vscodelc.LanguageClientOptions = {
        // Register the server for 'miniyaml' (.yaml) files. This uses the definition for 'miniyaml' found in package.json under 'contributes.languages'.
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
        outputChannelName: "OpenRA IDE",
    };

    let client = new vscodelc.LanguageClient('oraide', 'OpenRA IDE', serverOptions, clientOptions, true);

    let disposable = client.start();

    // Push the disposable to the context's subscriptions so that the
    // client can be deactivated on extension deactivation
    context.subscriptions.push(disposable);
}
