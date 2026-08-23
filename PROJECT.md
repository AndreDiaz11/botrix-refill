# Botrix Refill

## Qué es
App de escritorio Windows que monitorea la tienda de puntos de un streamer en botrix.live. Muestra las recompensas agrupadas por tipo con estado de stock y código de canje (copiable con un clic), los puntos del usuario con actualización casi en tiempo real, y envía notificaciones nativas + Telegram cuando la tienda se rellena. Una sola cuenta y tienda monitoreada a la vez.

## Cómo se ve y funciona
1. **Setup** (siempre al abrir): tarjeta con 3 secciones tipo acordeón — **Streamer** (abierta por defecto), **Session-kid** y **Telegram** (colapsadas, un clic en el título las despliega). Telegram vive únicamente aquí — toggle + Bot Token + Chat ID + botón "Probar conexión". Sin textos de ayuda extra, solo las secciones y el botón "▶ Iniciar monitoreo".
2. **Shop**: header con gradiente azul, avatar/nombre/puntos hero (22px bold) + nivel + botón para abrir la tienda del streamer. Botón ↻ junto a los puntos con cooldown 10s. Botón ⏹ Detener en la esquina superior derecha — **ya no hay botón de Telegram/Configuración aquí**, para cambiar streamer o Telegram hay que volver al Setup (Detener → Cambiar streamer).
3. **Recompensas agrupadas por tipo** (secciones fijas, no colapsables): "Yape / Plin", "Suscripciones", "Recargas", "Otros" — detectado por palabras clave en nombre/código del item. Cada card: imagen | nombre | código de canje (`!code`, clic para copiar) | precio | badge de stock.
4. **Pausa**: al detener muestra pantalla con "Cambiar streamer" o "Reanudar".
5. **Polling tienda**: cada 10-14s (jitter aleatorio, evita patrón detectable y reduce riesgo de bloqueo). Si hay errores de red consecutivos, el intervalo crece exponencialmente hasta 60s (backoff) y se resetea al recuperar conexión. **Puntos**: cada 60s + botón manual con cooldown 10s. La barra inferior muestra esto en texto claro ("Monitoreando · tienda cada 10-14s, puntos cada 60s · última revisión HH:MM:SS") y los chips dicen "X recompensas disponibles" / "Y recompensas en total" (antes decía solo "disp."/"total").
6. **Refill detectado**: notificación nativa Windows + mensaje Telegram.
7. **Tray**: ícono hexágono azul de marca. Al cerrar con X se oculta pero sigue monitoreando. Clic derecho → Salir para cerrar.
8. **Arranque**: la ventana se crea oculta (`show:false`) y solo se muestra al evento `ready-to-show` — evita el parpadeo blanco mientras carga. Build empaquetado con `asar:true` explícito para que el exe portátil se auto-extraiga más rápido. Nota: la demora real de la primera apertura de cada versión del exe es del propio formato "portable" de electron-builder (se auto-descomprime la primera vez que Windows ve esa versión) + el escaneo de Defender/SmartScreen a un exe sin firmar — no se puede eliminar del todo sin pasar a instalador NSIS o firmar el exe, ninguna de las dos deseadas por el momento. Sigue siendo un único exe portátil, sin instalador — se puede pasar a cualquiera y correr con su propia cuenta sin instalar nada.

## Stack
- **Electron 28** — ventana sin bordes, tray, notificaciones nativas, proceso main
- **React 18 + electron-vite** — UI renderer
- **fetch nativo Node 18** — llamadas REST a botrix.live y Telegram Bot API
- **fs nativo** — config persistida en JSON local

## Estructura
```
Botrix Refill/
├── Botrix Refill.exe          ← compilado portátil (SIEMPRE reemplazar este, nunca duplicar)
└── project/
    ├── resources/
    │   ├── icon.png           ← 256×256 icono app (hexágono azul #2563eb en cuadrado redondeado con gradiente)
    │   ├── tray.png           ← 32×32 icono tray (hexágono azul sólido, transparente)
    │   └── tray16.png         ← 16×16 fallback (mismo estilo)
    ├── src/
    │   ├── main/
    │   │   ├── index.js       ← BrowserWindow (show:false + ready-to-show), Tray, IPC handlers
    │   │   ├── poller.js      ← polling con jitter 10-14s + backoff exponencial en errores
    │   │   ├── telegram.js    ← sendMessage Bot API
    │   │   └── store.js       ← load/save JSON AppData
    │   ├── preload/
    │   │   └── index.js       ← expone window.api al renderer
    │   └── renderer/
    │       ├── index.html     ← CSP con img-src *
    │       └── src/
    │           ├── App.jsx            ← estados: Setup / Shop / Paused
    │           ├── App.css            ← tema claro, paleta azul/verde/gold, accordion, reward-group
    │           ├── utils.js           ← extractName() compartida
    │           ├── pages/
    │           │   ├── Setup.jsx      ← acordeón Streamer/Session-kid/Telegram
    │           │   └── Shop.jsx       ← items agrupados + polling + pts refresh (sin SettingsPanel)
    │           └── components/
    │               ├── RewardCard.jsx ← card con código de canje copiable al portapapeles
    │               ├── UserHeader.jsx ← puntos hero + btn refresh cooldown
    │               └── StatusBar.jsx  ← dot + texto de intervalos + chips claros
    ├── electron.vite.config.js
    └── package.json
```

**Nota:** `project/node_modules`, `project/out` y `project/dist-electron` se eliminan tras cada compilación exitosa (regenerables con `npm install` / `npm run package`).

## Instalar y correr
```bash
cd project
npm install         # necesario la primera vez (node_modules fue limpiado)
npm run dev          # desarrollo con HMR
npm run package       # genera exe en dist-electron/ → moverlo manualmente a la raíz y borrar dist-electron/
```

## Env vars
No requiere. Config en: `C:\Users\{usuario}\AppData\Roaming\botrix-refill\botrix-refill-config.json`

## Estado
Funcional: **sí** | Beta: **sí** | Última revisión: rediseño visual completo — Setup en acordeón, Telegram solo en Setup, recompensas agrupadas por tipo, barra de estado con texto claro, paleta azul (antes morado)

## Integraciones externas
| Servicio | Endpoint | Auth | Uso |
|---|---|---|---|
| botrix.live shop | `/api/public/shop/items?u={streamer}&platform=kick` | ninguna | items + stock + `code` (comando de canje, ej. `yaplin5`) + `description` (instrucciones de uso, no mostrada en UI actualmente) |
| botrix.live user | `/api/public/leaderboard/whoamiKick?user={streamer}&t={ts}` | Header `Session-kid` | puntos + nivel + avatar |
| Telegram Bot API | `POST /bot{token}/sendMessage` | token en URL | notificación de refill |

**Session-kid**: F12 → Application → Local Storage en botrix.live

**Sobre el campo `code`**: cada item de la API trae un `code` corto (ej. `yaplin5`). En el chat del streamer se canjea escribiendo `!` + ese código (ej. `!yaplin5`). La app lo muestra como pill azul junto a cada recompensa; un clic lo copia al portapapeles con feedback "✓ Copiado". El campo `description` de la API trae texto más largo con reglas de uso (límites, qué agregar al comando) — no se muestra todavía, se puede agregar como tooltip o línea extra si se pide a futuro.

**Agrupación de recompensas**: sin categoría real en la API, se detecta por palabras clave en `code`+`name` (minúsculas): contiene "yape"/"plin" → *Yape / Plin*; contiene "sub" → *Suscripciones*; contiene "bet"/"recarga" → *Recargas*; el resto → *Otros*. Función `groupItems()` en `Shop.jsx`. Si otro streamer usa nombres de items muy distintos, estas palabras clave pueden no matchear bien — ajustar el array `GROUPS` en ese archivo.

## Escalabilidad
- Una cuenta/tienda a la vez. Para múltiples: array de configs + múltiples pollers + tabs en UI.
- Nuevo tipo de notificación: agregar handler en `main/index.js` y llamarlo desde `poller.js`.
- Cambiar rango de polling: constantes `MIN_DELAY`/`MAX_DELAY`/`MAX_BACKOFF` en `poller.js` (actualmente 10000/14000/60000 ms).
- Nuevo grupo de recompensas o ajustar keywords: array `GROUPS` en `Shop.jsx`.
- Mostrar la `description` completa de cada item: pasarla como prop a `RewardCard` y agregarla como tooltip o línea colapsable (decisión pendiente de diseño si se pide).

## Compatibilidad
Solo Windows x64 (portable exe, sin instalador — se distribuye copiando el .exe, cada usuario lo corre con su propia cuenta).

## Versión: 1.7.0

## Cambios recientes
1. [1.7.0] Paleta de color cambiada de morado (#6c47ff) a azul (#2563eb) en toda la UI, íconos de app y tray regenerados a juego
2. [1.7.0] Setup rediseñado como acordeón de 3 secciones (Streamer abierta por defecto, Session-kid y Telegram colapsadas), sin textos de ayuda extra
3. [1.7.0] Telegram removido por completo del header de Shop (ya no hay botón ahí); vive únicamente en el Setup inicial
4. [1.7.0] Recompensas agrupadas por tipo (Yape/Plin, Suscripciones, Recargas, Otros) detectado por palabras clave, en vez de lista plana
5. [1.7.0] StatusBar con texto claro de los intervalos de polling reales y chips renombrados ("X recompensas disponibles" / "Y recompensas en total" en vez de "disp."/"total")
6. [1.6.2] Arranque optimizado: ventana oculta hasta `ready-to-show` (sin flash blanco) + `asar:true` explícito en el build para extracción más rápida del exe portátil
7. [1.6.2] Limpieza de CSS muerto: `.btn-secondary` y `.btn-stop` eliminadas de App.css
8. [1.6.1] Código de canje (`!code`) visible y copiable con un clic en cada recompensa
9. [1.6.1] Polling de tienda con jitter aleatorio 10-14s (antes fijo 12s) + backoff exponencial en errores de red
10. [1.6.0] Icono app 256×256 + icono tray 32×32 (ambos hoy en azul, ver #1)
