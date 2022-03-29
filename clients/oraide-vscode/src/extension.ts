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

    // Just a sanity check for the workspace folder.
    if (vscode.workspace.workspaceFolders === undefined || vscode.workspace.workspaceFolders.length === 0) {
        vscode.window.showInformationMessage('Something went wrong. Extension ORAIDE will abort.');
        return;
    }

    // Explicitly not supporting multi-root workspaces for the moment. May change later depending on requirements.
    // https://code.visualstudio.com/docs/editor/multi-root-workspaces
    // TODO: What if the workspace changes during execution??
    const workspaceFolderPath = vscode.workspace.workspaceFolders[0].uri.fsPath;

    // Try to run the language server.
    const config = vscode.workspace.getConfiguration();
    let result = await languageServer.tryStart(context, config, workspaceFolderPath);
    if (result.isSuccessful) {
        client = result.client!;
    }

    handleMiniYamlFiles();
}

// this method is called when your extension is deactivated
export function deactivate() {
    if (!client) {
        return undefined;
    }

    logger.appendLine('Client stopping...');

    return client.stop();
}

function handleMiniYamlFiles() {
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