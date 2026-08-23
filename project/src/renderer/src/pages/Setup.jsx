import { useState } from 'react'

function Section({ title, open, onToggle, children }) {
  return (
    <div className="accordion-section">
      <button type="button" className="accordion-head" onClick={onToggle}>
        <span>{title}</span>
        <span className={`accordion-chev${open ? ' open' : ''}`}>▾</span>
      </button>
      {open && <div className="accordion-body">{children}</div>}
    </div>
  )
}

export default function Setup({ saved, onSave }) {
  const [streamer, setStreamer] = useState(saved?.streamer || '')
  const [sessionKid, setSessionKid] = useState(saved?.sessionKid || '')
  const [telegramEnabled, setTelegramEnabled] = useState(saved?.telegramEnabled || false)
  const [telegramToken, setTelegramToken] = useState(saved?.telegramToken || '')
  const [telegramChatId, setTelegramChatId] = useState(saved?.telegramChatId || '')
  const [openSection, setOpenSection] = useState('streamer')
  const [loading, setLoading] = useState(false)
  const [testLoading, setTestLoading] = useState(false)
  const [error, setError] = useState('')
  const [testMsg, setTestMsg] = useState('')

  function toggle(name) { setOpenSection(s => s === name ? '' : name) }

  async function handleSave(e) {
    e.preventDefault()
    if (!streamer.trim() || !sessionKid.trim()) { setError('Completa el streamer y Session-kid.'); return }
    setLoading(true); setError('')
    try {
      const cfg = {
        streamer: streamer.trim(),
        sessionKid: sessionKid.trim(),
        telegramEnabled,
        telegramToken: telegramToken.trim(),
        telegramChatId: telegramChatId.trim()
      }
      await window.api.saveConfig(cfg)
      onSave(cfg)
    } catch { setError('Error al guardar.') }
    finally { setLoading(false) }
  }

  async function handleTest() {
    if (!telegramToken || !telegramChatId) { setTestMsg('❌ Ingresa token y chat ID.'); return }
    setTestLoading(true); setTestMsg('')
    try { await window.api.testTelegram({ token: telegramToken.trim(), chatId: telegramChatId.trim() }); setTestMsg('✅ Enviado!') }
    catch (e) { setTestMsg(`❌ ${e.message}`) }
    finally { setTestLoading(false) }
  }

  return (
    <div className="setup-page">
      <form className="setup-card" onSubmit={handleSave}>
        <div className="setup-head">
          <div className="setup-icon">⬡</div>
          <div className="setup-title">Botrix Refill</div>
        </div>

        <Section title="Streamer" open={openSection === 'streamer'} onToggle={() => toggle('streamer')}>
          <div className="form-group">
            <input className="form-input" placeholder="mrchoco o link de botrix.live" value={streamer} onChange={e => setStreamer(e.target.value)} />
          </div>
        </Section>

        <Section title="Session-kid" open={openSection === 'sessionKid'} onToggle={() => toggle('sessionKid')}>
          <div className="form-group">
            <input className="form-input" placeholder="b9d790..." value={sessionKid} onChange={e => setSessionKid(e.target.value)} />
          </div>
        </Section>

        <Section title="Telegram" open={openSection === 'telegram'} onToggle={() => toggle('telegram')}>
          <div className="toggle-row">
            <span className="toggle-label">Activar notificaciones</span>
            <label className="toggle">
              <input type="checkbox" checked={telegramEnabled} onChange={e => setTelegramEnabled(e.target.checked)} />
              <span className="toggle-track" />
            </label>
          </div>
          {telegramEnabled && (<>
            <div className="form-group">
              <input className="form-input" placeholder="Bot Token" value={telegramToken} onChange={e => setTelegramToken(e.target.value)} />
            </div>
            <div className="form-group">
              <input className="form-input" placeholder="Chat ID" value={telegramChatId} onChange={e => setTelegramChatId(e.target.value)} />
            </div>
            <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
              <button type="button" className="btn btn-ghost btn-sm" onClick={handleTest} disabled={testLoading}>
                {testLoading ? 'Enviando...' : 'Probar conexión'}
              </button>
              {testMsg && <span style={{ fontSize: 11, color: testMsg.startsWith('✅') ? '#0d9668' : '#e0344c' }}>{testMsg}</span>}
            </div>
          </>)}
        </Section>

        {error && <div className="msg-error">{error}</div>}

        <button className="btn btn-primary" type="submit" disabled={loading} style={{ marginTop: 16 }}>
          {loading ? 'Cargando...' : '▶  Iniciar monitoreo'}
        </button>
      </form>
    </div>
  )
}
