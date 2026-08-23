import { useState } from 'react'

export default function UserHeader({ user, streamer, onOpenShop, onRefreshPts, ptsCooldown }) {
  const [imgErr, setImgErr] = useState(false)
  if (!user) return (
    <div className="user-block">
      <div className="user-ava-ph">?</div>
      <div className="user-info">
        <div className="user-name" style={{ color: 'rgba(255,255,255,0.5)', fontSize: 13 }}>Cargando...</div>
      </div>
    </div>
  )
  const initial = user.name?.[0]?.toUpperCase() ?? '?'
  return (
    <div className="user-block">
      {!imgErr && user.picture
        ? <img className="user-ava" src={user.picture} alt={user.name} referrerPolicy="no-referrer" onError={() => setImgErr(true)} />
        : <div className="user-ava-ph">{initial}</div>}
      <div className="user-info">
        <div className="user-name">{user.name}</div>
        <div className="user-pts-big">
          <span className="user-pts-coin">⬡</span>
          {user.points?.toLocaleString('es-PE') ?? '—'}
          <span className="pts-label">puntos</span>
          <button className="btn-pts-refresh" onClick={onRefreshPts} disabled={ptsCooldown > 0} title="Actualizar puntos">
            {ptsCooldown > 0 ? `${ptsCooldown}s` : '↻'}
          </button>
        </div>
        <div className="user-row">
          {user.level && <span className="user-lvl">Nv. {user.level}</span>}
          <button className="btn-shop-link" onClick={onOpenShop}>🔴 {streamer}</button>
        </div>
      </div>
    </div>
  )
}
