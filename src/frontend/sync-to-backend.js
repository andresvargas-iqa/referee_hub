#!/usr/bin/env node
const fs = require('fs');
const path = require('path');

const frontendDist = path.resolve(__dirname, 'dist');
const backendWwwroots = [
  path.resolve(__dirname, '..', 'backend', 'ManagementHub.Service', 'bin', 'Debug', 'net8.0', 'wwwroot'),
  path.resolve(__dirname, '..', 'backend', 'ManagementHub.Service', 'bin', 'Release', 'net8.0', 'wwwroot'),
];

if (!fs.existsSync(frontendDist)) {
  console.error('Frontend dist not found');
  process.exit(1);
}

function copyRecursive(src, dest) {
  if (!fs.existsSync(dest)) fs.mkdirSync(dest, { recursive: true });
  fs.readdirSync(src).forEach(file => {
    const srcPath = path.join(src, file);
    const destPath = path.join(dest, file);
    fs.statSync(srcPath).isDirectory()
      ? copyRecursive(srcPath, destPath)
      : fs.copyFileSync(srcPath, destPath);
  });
}

backendWwwroots.forEach(wwwroot => {
  if (!fs.existsSync(wwwroot)) fs.mkdirSync(wwwroot, { recursive: true });
  fs.readdirSync(wwwroot).forEach(entry => {
    fs.rmSync(path.join(wwwroot, entry), { recursive: true, force: true });
  });
  copyRecursive(frontendDist, wwwroot);
  console.log('✅', wwwroot);
});
