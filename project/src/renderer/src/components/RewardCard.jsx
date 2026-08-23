import { useState } from 'react'

export default function RewardCard({ item }) {
  const { name, price, stock, image, disponibilidad, code } = item
  const [copied, setCopied] = useState(false)

  let badge = 'b-no', label = 'Sin stock', cls = 'c-no'

  if (stock === -1)      { badge = 'b-inf'; label = '∞ Ilimitado'; cls = 'c-inf' }
  else if (stock > 0)    { badge = 'b-ok';  label = stock < 999 ? `×${stock} stock` : 'Disponible'; cls = 'c-ok' }
  else if (disponibilidad === 'Solo para suscriptores') { badge = 'b-sub'; label = '👑 Solo subs'; cls = 'c-sub' }

  function handleCopy() {
    navigator.clipboard.writeText(`!${code}`)
    setCopied(true)
    setTimeout(() => setCopied(false), 1200)
  }

  return (
    <div className={`card ${cls}`}>
      <div className="card-img">
        {image
          ? <img src={image} alt={name} loading="lazy" referrerPolicy="no-referrer" />
          : <div className="card-img-ph">🎁</div>}
      </div>

      <div className="card-name">{name}</div>

      {code && (
        <button className={`card-code${copied ? ' copied' : ''}`} onClick={handleCopy} title="Copiar comando">
          {copied ? '✓ Copiado' : `!${code}`}
        </button>
      )}

      <div className="card-price-col">
        <span className="card-price">{price.toLocaleString('es-PE')}</span>
        <span className="card-pts">puntos</span>
      </div>

      <div className="card-stock">
        <span className={`badge ${badge}`}>{label}</span>
      </div>
    </div>
  )
}
