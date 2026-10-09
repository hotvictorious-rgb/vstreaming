const { app, BrowserWindow, dialog, Menu } = require('electron');
const path = require('path');
const http = require('http');

// Enforce single instance lock
const gotTheLock = app.requestSingleInstanceLock();
if (!gotTheLock) {
    app.quit();
    process.exit(0);
}

// Start internal broadcast engine (server.js)
let serverModule = null;
try {
    serverModule = require('./server.js');
} catch (err) {
    console.error('[Victorious Studio Engine Error]:', err);
}

let mainWindow = null;
let isQuitting = false;

// Poll internal HTTP server until ready
function waitForServer(port, callback, timeout = 20000) {
    const start = Date.now();
    const interval = setInterval(() => {
        const req = http.get(`http://localhost:${port}/`, (res) => {
            clearInterval(interval);
            callback(true);
        });
        req.on('error', () => {
            if (Date.now() - start > timeout) {
                clearInterval(interval);
                callback(false);
            }
        });
    }, 300);
}

function createWindow() {
    mainWindow = new BrowserWindow({
        width: 1366,
        height: 850,
        minWidth: 1024,
        minHeight: 700,
        title: 'Victorious Streaming Hub - Church Broadcast Studio',
        backgroundColor: '#0f0f13',
        autoHideMenuBar: true,
        show: false,
        webPreferences: {
            nodeIntegration: false,
            contextIsolation: true,
            devTools: false
        }
    });

    // Remove top menu bar for clean software feel
    mainWindow.setMenuBarVisibility(false);
    Menu.setApplicationMenu(null);

    // Wait for server to bind then load dashboard
    waitForServer(3000, () => {
        mainWindow.loadURL('http://localhost:3000');
    });

    mainWindow.once('ready-to-show', () => {
        mainWindow.show();
        mainWindow.focus();
    });

    // Safety guard against accidental closure during service
    mainWindow.on('close', (e) => {
        if (!isQuitting) {
            e.preventDefault();
            const choice = dialog.showMessageBoxSync(mainWindow, {
                type: 'warning',
                buttons: ['Yes, Close Studio', 'Cancel (Keep Running)'],
                defaultId: 1,
                cancelId: 1,
                title: 'Confirm Studio Exit',
                message: 'Live Broadcast in Progress',
                detail: 'Are you sure you want to exit Victorious Streaming Hub? All active camera feeds and OBS connections will disconnect.'
            });

            if (choice === 0) {
                isQuitting = true;
                mainWindow.destroy();
                app.quit();
            }
        }
    });

    mainWindow.on('closed', () => {
        mainWindow = null;
    });
}

// Bring existing window to front if user attempts second launch
app.on('second-instance', () => {
    if (mainWindow) {
        if (mainWindow.isMinimized()) mainWindow.restore();
        mainWindow.focus();
    }
});

app.whenReady().then(() => {
    createWindow();

    app.on('activate', () => {
        if (BrowserWindow.getAllWindows().length === 0) createWindow();
    });
});

app.on('window-all-closed', () => {
    if (process.platform !== 'darwin') {
        app.quit();
    }
});

app.on('will-quit', () => {
    process.exit(0);
});