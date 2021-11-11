'use strict';

import * as vscode from 'vscode';
import * as fileSystem from 'fs';

// Taken from https://stackoverflow.com/questions/6889470/how-to-programmatically-detect-debug-mode-in-nodejs
export const IS_DEBUG = /--debug|--inspect/.test(process.execArgv.join(' '));

export async function getCurrentServerVersion(extensionStorageFolder: vscode.Uri): Promise<string> {
    let languageServerRootFolder = vscode.Uri.joinPath(extensionStorageFolder, 'LanguageServer');
    if (!fileSystem.existsSync(languageServerRootFolder.toString())) {
        await vscode.workspace.fs.createDirectory(languageServerRootFolder);
    }

    let content = await vscode.workspace.fs.readDirectory(languageServerRootFolder);
    let folders = content.filter(x => x[1] === 2) // TODO: There has to be a less stupid way of filtering just the subdirectories.
        .map(x => x[0])
        .sort((a, b) => a.localeCompare(b));

    return folders.pop() || ''; // Fall back to an empty string if no version strings were found to simplify later comparisson.
}
