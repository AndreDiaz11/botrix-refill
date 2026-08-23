import { sendMessage } from './telegram'

let timeoutId = null
let previousStock = {}
let activeConfig = {}
let consecutiveErrors = 0

const MIN_DELAY = 10000
const MAX_DELAY = 14000
const MAX_BACKOFF = 60000

function nextDelay() {
  const jitter = MIN_DELAY + Math.random() * (MAX_DELAY - MIN_DELAY)
  if (consecutiveErrors === 0) return jitter
  return Math.min(jitter * 2 ** consecutiveErrors, MAX_BACKOFF)
}

export function extractStreamer(input) {
  if (!input) return ''
  const match = input.match(/botrix\.live\/k\/([^/]+)/)
  return match ? match[1] : input.trim()
}

export async function fetchShopItems(streamer) {
  const res = await fetch(
    `https://botrix.live/api/public/shop/items?u=${encodeURIComponent(streamer)}&platform=kick`
  )
  if (!res.ok) throw new Error('Canal no encontrado')
  return res.json()
}

export function stopPolling() {
  if (timeoutId) {
    clearTimeout(timeoutId)
    timeoutId = null
  }
  previousStock = {}
  consecutiveErrors = 0
}

export function startPolling(cfg, onRefill, onUpdate) {
  activeConfig = { ...cfg, streamer: extractStreamer(cfg.streamer) }
  stopPolling()

  async function poll() {
    try {
      const items = await fetchShopItems(activeConfig.streamer)
      consecutiveErrors = 0
      const newStock = {}
      const refilled = []

      for (const item of items) {
        newStock[item.code] = item.stock
        const hadNoStock = previousStock[item.code] === 0
        const nowHasStock = item.stock !== 0
        const hasPrevData = Object.keys(previousStock).length > 0
        if (hasPrevData && hadNoStock && nowHasStock) refilled.push(item)
      }

      previousStock = newStock
      onUpdate(items, new Date().toISOString())

      for (const item of refilled) {
        onRefill(item)
        if (activeConfig.telegramEnabled && activeConfig.telegramToken && activeConfig.telegramChatId) {
          const msg =
            `🟢 <b>${item.name}</b> ya está disponible!\n` +
            `💰 <b>${item.price.toLocaleString()}</b> puntos\n` +
            `📺 Tienda de <b>${activeConfig.streamer}</b>\n` +
            `🔗 https://botrix.live/k/${activeConfig.streamer}/shop`
          sendMessage(activeConfig.telegramToken, activeConfig.telegramChatId, msg).catch(() => {})
        }
      }
    } catch {
      consecutiveErrors++
    } finally {
      timeoutId = setTimeout(poll, nextDelay())
    }
  }

  poll()
}
