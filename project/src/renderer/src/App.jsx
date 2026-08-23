import { useState, useEffect } from 'react'
import Setup from './pages/Setup'
import Shop from './pages/Shop'
import { extractName } from './utils'

export default function App() {
  const [config, setConfig] = useState(null)
  const [ready, setReady] = useState(false)
  const [inShop, setInShop] = useState(false)
  const [stopped, setStopped] = useState(false)

  useEffect(() => {
    window.api.getConfig().then(cfg => { setConfig(cfg); setReady(true) })
  }, [])

  const tb = () => (
    <div className="titlebar">
      <div className="titlebar-left">
        <div className="titlebar-logo"><div className="titlebar-dot" />BOTRIX REFILL</div>
        {inShop && !stopped && config?.streamer &&
          <span className="titlebar-pill">@{extractName(config.streamer)}</span>}
      </div>
      <div className="titlebar-controls">
        <button className="titlebar-btn" onClick={() => window.api.minimizeWindow()}>─</button>
        <button className="titlebar-btn close" onClick={() => window.api.closeWindow()}>✕</button>
      </div>
    </div>
  )

  if (!ready) return <div className="app">{tb()}<div className="loading-screen"><div className="spinner" /></div></div>

  if (stopped) return (
    <div className="app">
      {tb()}
      <div className="paused">
        <div className="paused-ico">⏹</div>
        <div className="paused-t">Monitoreo detenido</div>
        <div className="paused-s">La app sigue en el tray pero no está revisando la tienda.</div>
        <div className="paused-btns">
          <button className="btn btn-ghost" onClick={() => { setStopped(false); setInShop(false) }}>↩ Cambiar streamer</button>
          <button className="btn btn-primary" style={{ width: 'auto' }} onClick={() => { setStopped(false); setInShop(true) }}>▶ Reanudar</button>
        </div>
      </div>
    </div>
  )

  if (inShop && config?.streamer && config?.sessionKid) return (
    <div className="app">
      {tb()}
      <Shop config={config} onStop={() => setStopped(true)} />
    </div>
  )

  return (
    <div className="app">
      {tb()}
      <Setup
        saved={config}
        onSave={cfg => { setConfig(cfg); setInShop(true); setStopped(false) }}
      />
    </div>
  )
}
