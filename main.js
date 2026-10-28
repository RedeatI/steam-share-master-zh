const { app, BrowserWindow, ipcMain, dialog, Tray, Menu, nativeImage } = require('electron');
const path = require('path');
const fs = require('fs');
const { exec, execFileSync } = require('child_process');

let mainWindow;
let tray = null;
const steamPathFile = path.join(app.getPath('userData'), 'steam_path.txt');
app.isQuitting = false;

function getSteamPath() {
    if (fs.existsSync(steamPathFile)) {
        const saved = fs.readFileSync(steamPathFile, 'utf-8').trim();
        if (saved && fs.existsSync(saved)) return saved;
    }
    try {
        const script = "[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new(); " +
            "$p = (Get-ItemProperty 'HKCU:\\Software\\Valve\\Steam' -ErrorAction SilentlyContinue).SteamExe; " +
            "if (-not $p -or -not (Test-Path -LiteralPath $p)) { $d = (Get-ItemProperty 'HKLM:\\SOFTWARE\\WOW6432Node\\Valve\\Steam' -ErrorAction SilentlyContinue).InstallPath; if ($d) { $p = Join-Path $d 'steam.exe' } }; " +
            "if ($p) { [Console]::Write($p) }";
        const detected = execFileSync('powershell.exe', ['-NoProfile', '-NonInteractive', '-Command', script], {
            encoding: 'utf8', windowsHide: true, timeout: 5000
        }).trim();
        if (detected && fs.existsSync(detected)) {
            fs.writeFileSync(steamPathFile, detected);
            return detected;
        }
    } catch (_) { /* Allow manual selection when detection fails. */ }
    return "C:\\Program Files (x86)\\Steam\\Steam.exe";
}

function sendLog(message) {
    if (mainWindow) {
        mainWindow.webContents.send('command-log', message);
    }
}

function executeBlockSteam() {
    const steamPath = getSteamPath();
    sendLog('正在断开 Steam 网络...');
    const commands = [
        'taskkill /F /IM "steam.exe" /IM "steamwebhelper.exe" /IM "steamservice.exe"',
        `netsh advfirewall firewall add rule name="BlockSteam" dir=out action=block program="${steamPath}" enable=yes`
    ];

    exec(commands[0], () => {
        setTimeout(() => {
            exec(commands[1], () => {
                sendLog("已执行断开操作。");
                setTimeout(() => relaunchSteam(steamPath), 1000);
            });
        }, 2000);
    });
}

function executeUnblockSteam() {
    const steamPath = getSteamPath();
    sendLog('正在恢复 Steam 网络...');
    const command = 'netsh advfirewall firewall set rule name="BlockSteam" new enable=no';

    exec(command, () => {
        setTimeout(() => {
            sendLog('已执行恢复操作。');
            relaunchSteam(steamPath);
        }, 1000);
    });
}

function relaunchSteam(steamPath) {
    sendLog("正在关闭并重启 Steam...");
    const killCommand = 'taskkill /F /IM "steam.exe" /IM "steamwebhelper.exe" /IM "steamservice.exe"';
    const startCommand = `start "" "${steamPath}"`;

    exec(killCommand, () => {

        setTimeout(() => {
            exec(startCommand);
        }, 3000);
    });
}

function createTray() {
    const icon = nativeImage.createFromPath(path.join(__dirname, 'assets/SSM-logo-png.ico'));
    tray = new Tray(icon);
    tray.setToolTip('Steam Share Master');

    const contextMenu = Menu.buildFromTemplate([
        {
            label: '显示主窗口',
            click: () => {
                mainWindow.show();
            }
        },
        { type: 'separator' },
        {
            label: '断开 Steam 网络',
            click: () => {
                executeBlockSteam();
            }
        },
        {
            label: '恢复 Steam 网络',
            click: () => {
                executeUnblockSteam();
            }
        },
        { type: 'separator' },
        {
            label: '开机启动',
            type: 'checkbox',
            checked: app.getLoginItemSettings().openAtLogin,
            click: () => {
                const settings = app.getLoginItemSettings();
                app.setLoginItemSettings({
                    openAtLogin: !settings.openAtLogin,
                    path: app.getPath('exe')
                });
            }
        },
        { type: 'separator' },
        {
            label: '退出',
            click: () => {
                app.isQuitting = true;
                app.quit();
            }
        },
        {
            label: '帮助',
            click: () => {
                const { shell } = require('electron');
                shell.openExternal('https://github.com/p4ul-t/steam-share-master-ssm/blob/main/find-Steam.exe.md');
            }
        },
    ]);

    tray.setContextMenu(contextMenu);

    tray.on('click', () => {
        mainWindow.isVisible() ? mainWindow.hide() : mainWindow.show();
    });
}

function createWindow() {
    mainWindow = new BrowserWindow({
        width: 380,
        height: 400,
        resizable: false,
        webPreferences: {
            preload: path.join(__dirname, 'preload.js'),
            contextIsolation: true,
            nodeIntegration: false
        },
        icon: path.join(__dirname, 'assets/SSM-logo-png.ico'),
        title: "Steam Share Master - SSM"
    });

    mainWindow.setMenuBarVisibility(false);
    mainWindow.loadFile('index.html');

    mainWindow.on('close', (event) => {
        if (!app.isQuitting) {
            event.preventDefault();
            mainWindow.hide();
        }
    });
}

app.whenReady().then(() => {
    createWindow();
    createTray();

    app.on('activate', () => {
        if (BrowserWindow.getAllWindows().length === 0) {
            createWindow();
        } else {
            mainWindow.show();
        }
    });
});

app.on('window-all-closed', () => {
    if (process.platform !== 'darwin') {
    }
});

ipcMain.handle('get-initial-path', () => {
    return getSteamPath();
});

ipcMain.handle('select-steam-path', async () => {
    const result = await dialog.showOpenDialog(mainWindow, {
        properties: ['openFile'],
        title: '选择 Steam.exe',
        defaultPath: 'C:\\',
        filters: [{ name: '可执行文件', extensions: ['exe'] }]
    });

    if (!result.canceled && result.filePaths.length > 0) {
        const newPath = result.filePaths[0];
        fs.writeFileSync(steamPathFile, newPath);
        return newPath;
    }
    return null;
});

ipcMain.on('block-steam', () => {
    executeBlockSteam();
});

ipcMain.on('unblock-steam', () => {
    executeUnblockSteam();
});
