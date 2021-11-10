'use strict';

const fetch = require('node-fetch');

import * as fileSystem from 'fs';
import * as path from 'path';
import * as vscode from 'vscode';
import * as util from 'util';
import * as extract from 'extract-zip';

const streamPipeline = util.promisify(require('stream').pipeline)

export async function getLanguageServerAsset(): Promise<{ name: string; browser_download_url: vscode.Uri; } | undefined> {

    const headers: Record<string, string> = { Accept: "application/vnd.github.v3+json" };
    const requestUrl = "https://api.github.com/repos/penev92/Oraide.LanguageServer/releases/latest";
    const response = await (() => {
        return fetch(requestUrl, { headers: headers });
    })();

    let toJson = await response.json();
    let release: GithubRelease = toJson;
    return release.assets.find(x => x.name.startsWith('server-') && x.name.endsWith('.zip'));
};

export async function getLatestServerVersion(): Promise<string> {
    let asset = await getLanguageServerAsset();
    if (!asset) {
        return '';
    }

    let version = asset.name;
    version = version.substring(0, version.lastIndexOf('.'));
    return version;
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

    // TODO: This is bad and we should feel bad. Find a way to clean up this mess!
    let filePath = path.join(path.join(extensionStorageFolder.toString(), 'LanguageServer', asset.name));
    let parsed = vscode.Uri.parse(filePath);
    let toStringed = parsed.toString(true);
    let finalNonsense = toStringed.substring(9);

    await streamPipeline(response.body, fileSystem.createWriteStream(finalNonsense));

    let folderName = finalNonsense.substring(0, finalNonsense.length - 4);
    // if (!fileSystem.existsSync(folderName)) {
    //     await vscode.workspace.fs.createDirectory(vscode.Uri.parse(folderName));
    // }

    try {
        await extract(finalNonsense, { dir: folderName });
    } catch (error) {
        return false;
    }

    // Finally, delete the downloaded .zip file.
    fileSystem.unlinkSync(finalNonsense);

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