'use strict';

// Taken from https://stackoverflow.com/questions/6889470/how-to-programmatically-detect-debug-mode-in-nodejs
export const IS_DEBUG = /--debug|--inspect/.test(process.execArgv.join(' '));