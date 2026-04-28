#!/usr/bin/env node
/**
 * Syncs frontend build output to backend wwwroot for serving static assets.
 * This ensures CSS, images, and other static files are available when the backend serves the app.
 * Auto-generates CSS if not present.
 */

const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

function ensureCssExists() {
  const frontendDist = path.resolve(__dirname, 'dist');
  const cssFile = path.join(frontendDist, 'management_hub.css');
  const appCssFile = path.join(frontendDist, 'management_hub_app.css');

  // Check if CSS files exist, if not generate them
  if (!fs.existsSync(cssFile)) {
    try {
      console.log('📝 Generating management_hub.css...');
      execSync('npx postcss ./assets/stylesheets/application.css -o dist/management_hub.css', {
        cwd: __dirname,
        stdio: 'ignore',
      });
      console.log('✓ Generated management_hub.css');
    } catch (err) {
      console.warn('⚠️  Could not auto-generate management_hub.css:', err.message);
    }
  }

  if (!fs.existsSync(appCssFile)) {
    try {
      console.log('📝 Generating management_hub_app.css...');
      execSync('npx sass ./app/assets/stylesheets/app.scss dist/management_hub_app.css', {
        cwd: __dirname,
        stdio: 'ignore',
      });
      console.log('✓ Generated management_hub_app.css');
    } catch (err) {
      console.warn('⚠️  Could not auto-generate management_hub_app.css:', err.message);
    }
  }
}

function ensureImagesExist() {
  const frontendDist = path.resolve(__dirname, 'dist');
  const imagesSource = path.join(__dirname, 'assets', 'images');
  const imagesDest = path.join(frontendDist, 'images');

  if (!fs.existsSync(imagesDest) && fs.existsSync(imagesSource)) {
    try {
      console.log('📁 Copying images to dist...');
      copyDirSync(imagesSource, imagesDest);
      console.log('✓ Copied images to dist');
    } catch (err) {
      console.warn('⚠️  Could not copy images:', err.message);
    }
  }
}

function syncToBackend() {
  const frontendDist = path.resolve(__dirname, 'dist');
  const backendWwwroot = path.resolve(__dirname, '..', 'backend', 'ManagementHub.Service', 'bin', 'Debug', 'net8.0', 'wwwroot');

  // Check if dist folder exists
  if (!fs.existsSync(frontendDist)) {
    console.warn('⚠️  Frontend dist folder not found at', frontendDist);
    return;
  }

  // Check if backend wwwroot exists, create if needed
  if (!fs.existsSync(backendWwwroot)) {
    console.log('📁 Creating backend wwwroot directory:', backendWwwroot);
    fs.mkdirSync(backendWwwroot, { recursive: true });
  }

  // Copy CSS files
  const cssFiles = fs.readdirSync(frontendDist).filter(f => f.endsWith('.css'));
  cssFiles.forEach(file => {
    const src = path.join(frontendDist, file);
    const dest = path.join(backendWwwroot, file);
    try {
      fs.copyFileSync(src, dest);
      console.log('✓ Copied', file);
    } catch (err) {
      console.error('✗ Failed to copy', file, ':', err.message);
    }
  });

  // Copy images folder
  const imagesSource = path.join(frontendDist, 'images');
  const imagesDest = path.join(backendWwwroot, 'images');
  
  if (fs.existsSync(imagesSource)) {
    try {
      copyDirSync(imagesSource, imagesDest);
      console.log('✓ Copied images directory');
    } catch (err) {
      console.error('✗ Failed to copy images:', err.message);
    }
  }

  console.log('✅ Static assets synced to backend wwwroot');
}

/**
 * Recursively copy a directory
 */
function copyDirSync(src, dest) {
  // Create destination directory if it doesn't exist
  if (!fs.existsSync(dest)) {
    fs.mkdirSync(dest, { recursive: true });
  }

  // Copy all files in the source directory
  const files = fs.readdirSync(src);
  files.forEach(file => {
    const srcPath = path.join(src, file);
    const destPath = path.join(dest, file);

    if (fs.statSync(srcPath).isDirectory()) {
      // Recursively copy subdirectories
      copyDirSync(srcPath, destPath);
    } else {
      // Copy files
      fs.copyFileSync(srcPath, destPath);
    }
  });
}

// Run sync process
ensureCssExists();
ensureImagesExist();
syncToBackend();
