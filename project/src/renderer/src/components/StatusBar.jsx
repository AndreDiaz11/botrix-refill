export default function StatusBar({ lastUpdate, itemCount, polling }) {
  const fmt = iso => iso ? new Date(iso).toLocaleTimeString('es-PE', { hour: '2-digit', minute: '2-digit', second: '2-digit' }) : '—'
  const av = itemCount?.available ?? 0
  const tot = itemCount?.total ?? 0
  return (
    <div className="statusbar">
      <div className={`status-dot${polling ? '' : ' off'}`} />
      <span className="status-txt">
        {polling ? 'Monitoreando · tienda cada 10-14s, puntos cada 60s' : 'Detenido'} · última revisión {fmt(lastUpdate)}
      </span>
      <div className="status-chips">
        <span className="chip chip-ok">{av} recompensas disponibles</span>
        <span className="chip chip-tot">{tot} recompensas en total</span>
      </div>
    </div>
  )
}
