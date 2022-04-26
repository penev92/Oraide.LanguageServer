'use strict';

const fetch = require('node-fetch-retry-timeout');

import * as fileSystem from 'fs';
import * as path from 'path';
import * as vscode from 'vscode';
import * as util from 'util';
import * as extract from 'extract-zip';
import { pathToFileURL, fileURLToPath } from 'url';

const streamPipeline = util.promisify(require('stream').pipeline)

async function getLanguageServerAsset(): Promise<{ name: string; browser_download_url: vscode.Uri; } | undefined> {

    const headers: Record<string, string> = { Accept: "application/vnd.github.v3+json" };
    const requestUrl = "https://api.github.com/repos/penev92/Oraide.LanguageServer/releases/latest";
    const response = await fetch(requestUrl, {
        method: 'GET', 
        retry: 2, // number attempts to retry
        pause: 500, // pause between requests (ms)
        timeout: 5000,  // timeout PER 1 REQUEST (ms)
        headers: headers
    });

    let toJson = await response.json();
    let release: GithubRelease = toJson;
    return release.assets.find(x => x.name.startsWith('server-') && x.name.endsWith('.zip'));
};

export async function getLatestServerVersion(): Promise<string> {
    try {
        let asset = await getLanguageServerAsset();
        if (!asset) {
            return '';
        }

        let version = asset.name;
        version = version.substring(0, version.lastIndexOf('.'));
        return version;
    }
    catch (e) {
        return '';
    }

}

export async function downloadLanguageServer(context: vscode.ExtensionContext): Promise<boolean> {
    let asset = await getLanguageServerAsset();
    if (!asset) {
        return false;
    }

    let response = await fetch(asset.browser_download_url);
    if (!response.ok) {
        return false;
    }

    const extensionStorageFolder = context.globalStorageUri;
    let filePath = path.join(extensionStorageFolder.toString(true), 'LanguageServer', asset.name);
    let resolvedFilePath = fileURLToPath(filePath);

    // Write the downloaded zip file to disk.
    await streamPipeline(response.body, fileSystem.createWriteStream(resolvedFilePath));

    // Create the folder in which we will be extracting the language server.
    let folderPathUrl = pathToFileURL(resolvedFilePath.substring(0, resolvedFilePath.lastIndexOf('.')));
    if (!fileSystem.existsSync(folderPathUrl)) {
        fileSystem.mkdirSync(folderPathUrl);
    }

    // Extract the downloaded zip.
    await extract(resolvedFilePath, { dir: fileURLToPath(folderPathUrl) });

    // Finally, delete the downloaded .zip file.
    fileSystem.unlinkSync(resolvedFilePath);

    return true;
};

// We omit declaration of tremendous amount of fields that we are not using here
interface GithubRelease {
    name: string;
    id: number;
    // eslint-disable-next-line camelcase
    published_at: string;
    assets: Array<{
        name: string;
        // eslint-disable-next-line camelcase
        browser_download_url: vscode.Uri;
    }>;
}