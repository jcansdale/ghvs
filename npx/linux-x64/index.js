#!/usr/bin/env node

const { spawn } = require('child_process');
const path = require('path');

let ghvsFile = path.join(__dirname, "bin/ghvs");

const ghvs = spawn(ghvsFile, process.argv.slice(2));

ghvs.stdout.on('data', (data) => {
  console.log(`${data}`);
});

ghvs.stderr.on('data', (data) => {
  console.error(`${data}`);
});

ghvs.on('close', (code) => {
  process.exit(code)
});
