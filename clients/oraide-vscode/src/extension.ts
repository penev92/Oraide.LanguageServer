'use strict';

// The module 'vscode' contains the VS Code extensibility API
import * as vscode from 'vscode';
import * as vscodelc from 'vscode-languageclient/node';

import * as utils from './utils';
import { logger } from './logger';
import * as languageServer from './languageServer';

let client: vscodelc.LanguageClient;

export async function activate(context: vscode.ExtensionContext) {
    
    logger.appendLine(`Activating...`);

    if (utils.IS_DEBUG) {
        logger.appendLine(`This is running in DEBUG!`);
    } else {
        logger.appendLine("This is running for real!");
    }

    // Checking if the fallback OpenRA folder is configured.
    const config = vscode.workspace.getConfiguration();
    const gameFolderPath = config.get<string>('oraide.game.path');

    // Just a sanity check for the workspace folder.
    if (vscode.workspace.workspaceFolders === undefined || vscode.workspace.workspaceFolders.length === 0) {
        vscode.window.showInformationMessage('Something went wrong. Extension ORAIDE will abort.');
        return;
    }

    // Explicitly not supporting multi-root workspaces for the moment. May change later depending on requirements.
    // https://code.visualstudio.com/docs/editor/multi-root-workspaces
    const workspaceFolderPath = vscode.workspace.workspaceFolders[0].uri.fsPath;

    // TODO: What if the workspace changes during execution??

    // Locate language server binary.
    let serverPath = await languageServer.getLanguageServerPath(context, config);
    if (!serverPath) {
        logger.appendLine("Extension ORAIDE failed to find or download its language server and will abort.");
        logger.appendLine("If you are running in debug mode, configure the path to the language server in the extension settings!");
        vscode.window.showInformationMessage("Extension ORAIDE failed to find or download its language server and will abort.");
        return;
    }

    start(context, serverPath, workspaceFolderPath, gameFolderPath!);

    // Detect if a file identified as YAML is actually MiniYAML and switch if so.
    function switchLanguageIfMiniYAML(doc: vscode.TextDocument) {
        if (doc.languageId == 'yaml') {
            const text = doc.getText();
            if (text.includes('\t')) {
                vscode.languages.setTextDocumentLanguage(doc, 'miniyaml');
            }
        }
    }
    for (const editor of vscode.window.visibleTextEditors) {
        switchLanguageIfMiniYAML(editor.document);
    }
    vscode.workspace.onDidOpenTextDocument(switchLanguageIfMiniYAML);
}

// this method is called when your extension is deactivated
export function deactivate() {
    if (!client) {
        return undefined;
    }

    logger.appendLine('Client stopping...');

    return client.stop();
}

function start(context: vscode.ExtensionContext, serverPath: string, workspaceFolderPath: string, defaultOpenRaPath: string) {
    
    logger.appendLine(`METHOD start`);
    logger.appendLine(`  serverPath: ${serverPath}`);
    logger.appendLine(`  workspaceFolderPath: ${workspaceFolderPath}`);
    logger.appendLine(`  defaultOpenRaPath: ${defaultOpenRaPath}`);
    
    const serverOptions: vscodelc.ServerOptions = async () => languageServer.spawnServerProcess(serverPath, workspaceFolderPath, defaultOpenRaPath);

    const clientOptions: vscodelc.LanguageClientOptions = {
        // Register the server for 'yaml' (.yaml) files. This uses the definition for 'miniyaml' found in package.json under 'contributes.languages'.
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
        outputChannelName: "OpenRA IDE server",
    };

    let client = new vscodelc.LanguageClient('oraide', 'OpenRA IDE', serverOptions, clientOptions, true);

    let disposable = client.start();

    logger.appendLine("Started client")

    // Push the disposable to the context's subscriptions so that the
    // client can be deactivated on extension deactivation
    context.subscriptions.push(disposable);
}
