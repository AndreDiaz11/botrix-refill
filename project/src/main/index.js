import { app, BrowserWindow, ipcMain, Tray, Menu, nativeImage, Notification, shell } from 'electron'
import path from 'path'
import { load, save } from './store'
import { startPolling, stopPolling, fetchShopItems, extractStreamer } from './poller'
import { sendMessage } from './telegram'

let mainWindow = null
let tray = null
let isQuitting = false

function getTrayIcon() {
  try {
    const base = app.isPackaged ? process.resourcesPath : path.join(__dirname, '../../')
    const img = nativeImage.createFromPath(path.join(base, 'resources/tray.png'))
    return img.isEmpty() ? nativeImage.createFromPath(path.join(base, 'resources/tray16.png')) : img.resize({ width: 16, height: 16 })
  } catch {
    return nativeImage.createEmpty()
  }
}

function getAppIcon() {
  try {
    const base = app.isPackaged ? process.resourcesPath : path.join(__dirname, '../../')
    return path.join(base, 'resources/icon.png')
  } catch {
    return undefined
  }
}

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 920,
    height: 660,
    minWidth: 780,
    minHeight: 560,
    frame: false,
    backgroundColor: '#f2f3f9',
    icon: getAppIcon(),
    show: false,
    webPreferences: {
      preload: path.join(__dirname, '../preload/index.js'),
      contextIsolation: true,
      nodeIntegration: false
    },
    title: 'Botrix Refill'
  })

  mainWindow.once('ready-to-show', () => mainWindow.show())

  if (process.env.NODE_ENV === 'development') {
    mainWindow.loadURL(process.env.ELECTRON_RENDERER_URL || 'http://localhost:5173')
  } else {
    mainWindow.loadFile(path.join(__dirname, '../renderer/index.html'))
  }

  mainWindow.on('close', (e) => {
    if (!isQuitting) { e.preventDefault(); mainWindow.hide() }
  })
}

function createTray() {
  tray = new Tray(getTrayIcon())
  tray.setToolTip('Botrix Refill')
  tray.setContextMenu(Menu.buildFromTemplate([
    { label: 'Abrir Botrix Refill', click: () => mainWindow?.show() },
    { type: 'separator' },
    { label: 'Salir', click: () => { isQuitting = true; app.quit() } }
  ]))
  tray.on('click', () => {
    if (!mainWindow) return
    mainWindow.isVisible() ? mainWindow.hide() : mainWindow.show()
  })
}

app.whenReady().then(() => { createWindow(); createTray() })
app.on('before-quit', () => { isQuitting = true })
app.on('window-all-closed', () => { if (process.platform !== 'darwin') app.quit() })

ipcMain.handle('get-config', () => load())
ipcMain.handle('save-config', (_, cfg) => save(cfg))

ipcMain.handle('fetch-shop', (_, streamer) => fetchShopItems(extractStreamer(streamer)))

ipcMain.handle('fetch-user', async (_, { streamer, sessionKid }) => {
  const name = extractStreamer(streamer)
  const res = await fetch(
    `https://botrix.live/api/public/leaderboard/whoamiKick?user=${encodeURIComponent(name)}&t=${Date.now()}`,
    { headers: { 'Session-kid': sessionKid } }
  )
  if (!res.ok) throw new Error('Error al obtener puntos')
  return res.json()
})

ipcMain.handle('start-polling', (_, cfg) => {
  startPolling(
    cfg,
    (item) => {
      mainWindow?.webContents.send('item-refilled', item)
      try {
        new Notification({
          title: '🟢 Tienda actualizada — Botrix Refill',
          body: `${item.name} ya está disponible! (${item.price.toLocaleString()} puntos)`
        }).show()
      } catch {}
    },
    (items, lastUpdate) => mainWindow?.webContents.send('shop-updated', { items, lastUpdate })
  )
  return true
})

ipcMain.handle('stop-polling', () => { stopPolling(); return true })

ipcMain.handle('test-telegram', async (_, { token, chatId }) => {
  await sendMessage(token, chatId, '✅ <b>Botrix Refill</b> conectado correctamente!\nRecibirás notificaciones aquí cuando la tienda se rellene.')
  return true
})

ipcMain.handle('minimize-window', () => mainWindow?.minimize())
ipcMain.handle('close-window', () => mainWindow?.hide())
ipcMain.handle('open-external', (_, url) => shell.openExternal(url))
