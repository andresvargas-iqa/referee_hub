const Dotenv = require('dotenv-webpack');
const { merge } = require('webpack-merge');
const common = require('./webpack.config.common.js');

// Custom plugin to sync assets to backend after build
class SyncToBackendPlugin {
  apply(compiler) {
    compiler.hooks.done.tap('SyncToBackendPlugin', () => {
      try {
        require('./sync-to-backend.js');
      } catch (err) {
        console.error('Failed to sync assets to backend:', err.message);
      }
    });
  }
}

module.exports = merge(common, {
  entry: './app/index.tsx',
  mode: 'production',
  plugins: [
    new Dotenv({
      systemvars: true,
      path: './.env.prod',
      silent: true,
    }),
    new SyncToBackendPlugin(),
  ],
});