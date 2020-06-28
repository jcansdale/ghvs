module.exports = ghvs

function ghvs(args) {
  const package = findPackage()
  npx([package].concat(args));
}

function npx(args) {
  const npx = require('libnpx')
  const argv = [process.argv[0], process.argv[1]].concat(args);
  const parsed = npx.parseArgs(argv)
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
