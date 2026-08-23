import { contextBridge, ipcRenderer } from 'electron'

contextBridge.exposeInMainWorld('api', {
  getConfig: () => ipcRenderer.invoke('get-config'),
  saveConfig: (cfg) => ipcRenderer.invoke('save-config', cfg),
  fetchShop: (streamer) => ipcRenderer.invoke('fetch-shop', streamer),
  fetchUser: (params) => ipcRenderer.invoke('fetch-user', params),
  startPolling: (cfg) => ipcRenderer.invoke('start-polling', cfg),
  stopPolling: () => ipcRenderer.invoke('stop-polling'),
  testTelegram: (params) => ipcRenderer.invoke('test-telegram', params),
  minimizeWindow: () => ipcRenderer.invoke('minimize-window'),
  closeWindow: () => ipcRenderer.invoke('close-window'),
  openExternal: (url) => ipcRenderer.invoke('open-external', url),
  onShopUpdated: (cb) => {
    ipcRenderer.on('shop-updated', (_, data) => cb(data))
    return () => ipcRenderer.removeAllListeners('shop-updated')
  },
  onItemRefilled: (cb) => {
    ipcRenderer.on('item-refilled', (_, item) => cb(item))
    return () => ipcRenderer.removeAllListeners('item-refilled')
  }
})
