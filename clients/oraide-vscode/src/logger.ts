import * as vscode from 'vscode';

export const logger = vscode.window.createOutputChannel("OpenRA IDE extension");
logger.show();