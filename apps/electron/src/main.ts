import { app, BrowserWindow } from 'electron';
import { join } from 'node:path';

const angularDevServerUrl = 'http://localhost:4200';

const resolveProductionIndexHtml = () => {
  return app.isPackaged
    ? join(process.resourcesPath, 'app', 'browser', 'index.html')
    : join(__dirname, '..', '..', 'app', 'dist', 'app', 'browser', 'index.html');
};

const createWindow = () => {
  const window = new BrowserWindow({
    width: 1200,
    height: 800,
    minWidth: 900,
    minHeight: 600,
    webPreferences: {
      contextIsolation: true,
      preload: join(__dirname, 'preload.js'),
    },
  });

  if (!app.isPackaged) {
    void window.loadURL(process.env['ELECTRON_RENDERER_URL'] ?? angularDevServerUrl);
    return;
  }

  void window.loadFile(resolveProductionIndexHtml());
};

app.whenReady().then(() => {
  createWindow();

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow();
    }
  });
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});
