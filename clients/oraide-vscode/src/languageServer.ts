'use strict';

import * as vscode from 'vscode';
import { ChildProcess, spawn } from 'child_process';
import * as path from 'path';
import { fileURLToPath } from 'url';

import { logger } from './logger';
import * as serverDownload from './serverDownload';
import * as utils from './utils';

const LANGUAGE_SERVER_BINARY_NAME = "Oraide.LanguageServer.dll"

export async function getLanguageServerPath(context: vscode.ExtensionContext, config: vscode.WorkspaceConfiguration) : Promise<string | undefined> {
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
        const userResponse = await vscode.window.showInformationMessage("ORAIDE would like to download a newer version of its language server.", "Download now");
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

    let languageServerUri = path.join(globalStorageUri.toString(true), 'LanguageServer', currentServerVersion, LANGUAGE_SERVER_BINARY_NAME);
    let languageServerPath = fileURLToPath(languageServerUri);
    return languageServerPath;
};

export async function spawnServerProcess(serverPath: string, workspaceFolderPath: string, defaultOpenRaPath: string): Promise<ChildProcess> {

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

    if (utils.IS_DEBUG) {
        serverProcess.stderr.setEncoding('utf8');
        serverProcess.stderr.on('data', function (data) {
            logger.appendLine('stderr: ' + data);
        });

        serverProcess.on('close', function (code) {
            logger.appendLine('closing code: ' + code);
        });
    }

    return serverProcess;
};
