#!/usr/bin/env node

const package = findPackage()
const args = process.argv.slice(2)
npx(package, args);

function npx(package, args) {
  const npx = require('libnpx')
  const path = require('path')  
  const NPM_PATH = path.join(__dirname, 'node_modules', 'npm', 'bin', 'npm-cli.js')  
  const argv = [process.argv[0], process.argv[1], package].concat(args);
  const parsed = npx.parseArgs(argv, NPM_PATH)
  npx(parsed)  
}

function findPackage() {
  switch(process.platform) {
  case 'win32':
    return "@jcansdale/ghvs-win-x64";
  case 'linux':
    return "@jcansdale/ghvs-linux-x64";
  case 'darwin':
    return "@jcansdale/ghvs-osx-x64";
  }
}
