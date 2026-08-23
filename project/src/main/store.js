import { app } from 'electron'
import path from 'path'
import fs from 'fs'

const CONFIG_PATH = path.join(app.getPath('userData'), 'botrix-refill-config.json')

const defaults = {
  streamer: '',
  sessionKid: '',
  telegramToken: '',
  telegramChatId: '',
  telegramEnabled: false
}

export function load() {
  try {
    const raw = fs.readFileSync(CONFIG_PATH, 'utf-8')
    return { ...defaults, ...JSON.parse(raw) }
  } catch {
    return { ...defaults }
  }
}

export function save(data) {
  const merged = { ...load(), ...data }
  fs.writeFileSync(CONFIG_PATH, JSON.stringify(merged, null, 2), 'utf-8')
  return merged
}
