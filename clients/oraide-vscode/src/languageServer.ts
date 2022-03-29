'use strict';

import * as vscode from 'vscode';
import * as vscodelc from 'vscode-languageclient/node';
import { ChildProcess, spawn } from 'child_process';
import * as path from 'path';
import { fileURLToPath } from 'url';

import { logger } from './logger';
import * as serverDownload from './serverDownload';
import * as utils from './utils';

const LANGUAGE_SERVER_BINARY_NAME = "Oraide.LanguageServer.dll"

async function getLanguageServerPath(context: vscode.ExtensionContext, config: vscode.WorkspaceConfiguration) : Promise<string | undefined> {
    let serverPath = undefined;

    if (utils.IS_DEBUG) {
        serverPath = config.get<string>('oraide.server.path');
    } else {
        serverPath = await findOrDownloadLanguageServer(context);
    }

    return serverPath;
}

async function findOrDownloadLanguageServer(context: vscode.ExtensionContext): Promise<string | undefined> {
    const globalStorageUri = context.globalStorageUri;
    let currentServerVersion = await utils.getCurrentServerVersion(globalStorageUri);
    let latestServerVersion = await serverDownload.getLatestServerVersion();

    logger.appendLine("METHOD findOrDownloadLanguageServer");

    if (currentServerVersion === '' && latestServerVersion === '') {
        return undefined; // Nothing to do here - there is no language server and we couldn't find a version to download.
    }

    if (currentServerVersion === '') {
        logger.appendLine("NO SERVER FOUND");

        const userResponse = await vscode.window.showInformationMessage("ORAIDE needs to download the ORAIDE language server to function.", "Download now");
        if (userResponse !== "Download now") {
            return undefined;
        } else {
            if (await serverDownload.downloadLanguageServer(context)) {
                currentServerVersion = await utils.getCurrentServerVersion(globalStorageUri);
            } else {
                return undefined;
            }
        }
    }

    if (latestServerVersion.localeCompare(currentServerVersion) > 0) {
        if (await serverDownload.downloadLanguageServer(context)) {
            currentServerVersion = await utils.getCurrentServerVersion(globalStorageUri);
        }
    }

    let languageServerUri = path.join(globalStorageUri.toString(true), 'LanguageServer', currentServerVersion, LANGUAGE_SERVER_BINARY_NAME);
    let languageServerPath = fileURLToPath(languageServerUri);
    return languageServerPath;
}

async function spawnServerProcess(serverPath: string, workspaceFolderPath: string, defaultOpenRaPath: string): Promise<ChildProcess> {

    logger.appendLine(`METHOD spawnServerProcess`);

    // Path to the server binary (not executable because reasons).
    // Path to the currently open directory/workspace.
    // Path to the default/configured OpenRA folder (extension will let the user specify a path to it in extension settings).
    const command = "dotnet";
    const args = [
        serverPath,
        workspaceFolderPath,
        defaultOpenRaPath
    ];

    logger.appendLine(`ARGS: ${args.join(' ')}`);

    const options = {};

    const serverProcess = spawn(command, args, options);

    if (serverProcess && serverProcess.pid) {
        logger.appendLine(`Spawned language server process with PID ${serverProcess.pid}`);
    }

    serverProcess.on('error', (err: { code?: string; message: string }) => {
        logger.appendLine(`ERROR!!!`);
        logger.appendLine(err.message)
        if (err.code === 'ENOENT') {
            const msg = `Could not spawn oraide process: ${err.message}`;
            logger.appendLine(msg);
            vscode.window.showWarningMessage(msg);
        }
    });

    return serverProcess;
}

export async function tryStart(context: vscode.ExtensionContext, config: vscode.WorkspaceConfiguration,
    workspaceFolderPath: string) : Promise<{isSuccessful: boolean, client: vscodelc.LanguageClient | null}> {
    
    // Locate language server binary.
    let serverPath = await getLanguageServerPath(context, config);
    if (!serverPath) {
        logger.appendLine("Extension ORAIDE failed to find or download its language server and will abort.");
        logger.appendLine("If you are running in debug mode, configure the path to the language server in the extension settings!");
        vscode.window.showInformationMessage("Extension ORAIDE failed to find or download its language server and will abort.");
        return { isSuccessful: false, client: null };
    }

    // Try get the fallback OpenRA folder is configured.
    const defaultOpenRaPath = config.get<string>('oraide.game.path');

    logger.appendLine(`METHOD tryStart`);
    logger.appendLine(`  serverPath: ${serverPath}`);
    logger.appendLine(`  workspaceFolderPath: ${workspaceFolderPath}`);
    logger.appendLine(`  defaultOpenRaPath: ${defaultOpenRaPath}`);
    
    const serverOptions: vscodelc.ServerOptions = async () => spawnServerProcess(serverPath!, workspaceFolderPath, defaultOpenRaPath!);

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
        outputChannelName: "OpenRA IDE server",
    };

    let client = new vscodelc.LanguageClient('oraide', 'OpenRA IDE', serverOptions, clientOptions, utils.IS_DEBUG);

    let disposable = client.start();

    logger.appendLine("Started client")

    // Push the disposable to the context's subscriptions so that the
    // client can be deactivated on extension deactivation
    context.subscriptions.push(disposable);

    return { isSuccessful: true, client: client };
}
