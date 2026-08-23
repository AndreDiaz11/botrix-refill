import { useState, useEffect, useRef } from 'react'
import RewardCard from '../components/RewardCard'
import UserHeader from '../components/UserHeader'
import StatusBar from '../components/StatusBar'
import { extractName } from '../utils'

const GROUPS = [
  { label: 'Yape / Plin', match: k => k.includes('yape') || k.includes('plin') },
  { label: 'Suscripciones', match: k => k.includes('sub') },
  { label: 'Recargas', match: k => k.includes('bet') || k.includes('recarga') },
  { label: 'Otros', match: () => true }
]

function groupItems(items) {
  const buckets = GROUPS.map(g => ({ label: g.label, items: [] }))
  for (const item of items) {
    const key = `${item.code} ${item.name}`.toLowerCase()
    const bucket = buckets[GROUPS.findIndex(g => g.match(key))]
    bucket.items.push(item)
  }
  return buckets.filter(b => b.items.length > 0)
}

export default function Shop({ config, onStop }) {
  const [items, setItems] = useState([])
  const [user, setUser] = useState(null)
  const [lastUpdate, setLastUpdate] = useState(null)
  const [toasts, setToasts] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [polling, setPolling] = useState(false)
  const [ptsCooldown, setPtsCooldown] = useState(0)
  const tid = useRef(0)
  const cooldownRef = useRef(null)
  const streamer = extractName(config.streamer)

  useEffect(() => {
    load()
    window.api.startPolling(config).then(() => setPolling(true))
    const u1 = window.api.onShopUpdated(({ items: it, lastUpdate: ts }) => { setItems(it); setLastUpdate(ts) })
    const u2 = window.api.onItemRefilled(item => addToast(item))
    const ptsInterval = setInterval(refreshUser, 60000)
    return () => { window.api.stopPolling(); u1(); u2(); clearInterval(ptsInterval); clearInterval(cooldownRef.current) }
  }, [])

  async function load() {
    setLoading(true); setError('')
    try {
      const [shop, userData] = await Promise.all([
        window.api.fetchShop(config.streamer),
        window.api.fetchUser({ streamer: config.streamer, sessionKid: config.sessionKid })
      ])
      setItems(shop)
      setLastUpdate(new Date().toISOString())
      if (userData?.user) setUser(userData.user)
    } catch (e) { setError(e.message || 'Error al cargar') }
    finally { setLoading(false) }
  }

  async function refreshUser() {
    try {
      const userData = await window.api.fetchUser({ streamer: config.streamer, sessionKid: config.sessionKid })
      if (userData?.user) setUser(userData.user)
    } catch {}
  }

  function handleRefreshPts() {
    if (ptsCooldown > 0) return
    refreshUser()
    setPtsCooldown(10)
    clearInterval(cooldownRef.current)
    cooldownRef.current = setInterval(() => {
      setPtsCooldown(p => { if (p <= 1) { clearInterval(cooldownRef.current); return 0 } return p - 1 })
    }, 1000)
  }

  function addToast(item) {
    const id = ++tid.current
    setToasts(p => [...p, { id, item }])
    setTimeout(() => setToasts(p => p.filter(t => t.id !== id)), 5000)
  }

  async function handleStop() { await window.api.stopPolling(); setPolling(false); onStop() }

  const available = items.filter(i => i.stock !== 0).length
  const groups = groupItems(items)

  return (
    <div className="shop-page">
      <div className="shop-header">
        <div className="shop-header-row">
          <UserHeader user={user} streamer={streamer} onOpenShop={() => window.api.openExternal(`https://botrix.live/k/${streamer}/shop`)} onRefreshPts={handleRefreshPts} ptsCooldown={ptsCooldown} />
          <div className="shop-actions">
            <button className="btn btn-stop-hdr btn-sm" onClick={handleStop}>⏹ Detener</button>
          </div>
        </div>
        {error && <div className="msg-error" style={{ marginTop: 8 }}>{error}</div>}
      </div>

      <div className="shop-body">
        {loading && !items.length
          ? <div style={{ display: 'flex', justifyContent: 'center', padding: 40 }}><div className="spinner" /></div>
          : <div className="items-grid">
              {!items.length && <div className="no-items">No se encontraron items.</div>}
              {groups.map(g => (
                <div key={g.label} className="reward-group">
                  <div className="reward-group-label">{g.label}</div>
                  {g.items.map(item => <RewardCard key={item.code} item={item} />)}
                </div>
              ))}
            </div>}
      </div>

      <StatusBar lastUpdate={lastUpdate} itemCount={{ available, total: items.length }} polling={polling} />

      <div className="toasts">
        {toasts.map(({ id, item }) => (
          <div key={id} className="toast">
            <div className="toast-t">🟢 Tienda actualizada</div>
            <div className="toast-b"><b>{item.name}</b> · {item.price.toLocaleString('es-PE')} pts</div>
          </div>
        ))}
      </div>
    </div>
  )
}
