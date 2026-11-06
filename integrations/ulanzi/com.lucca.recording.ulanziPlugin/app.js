'use strict';
const fs = require('node:fs');
const path = require('node:path');
const {spawn} = require('node:child_process');
const config = JSON.parse(fs.readFileSync(path.join(__dirname, 'config.local.json'), 'utf8'));
if (!fs.existsSync(config.phoneDeck)) throw new Error('PhoneDeck executable missing');
const child = spawn(config.phoneDeck, ['--ulanzi-bridge', ...process.argv.slice(2)], {windowsHide:true, stdio:'ignore', cwd:path.dirname(config.phoneDeck)});
process.on('exit', () => child.kill());
child.on('exit', () => process.exit());