# Botrix Refill

## Qué es
App de escritorio Windows que monitorea la tienda de puntos de un streamer en botrix.live. Muestra las recompensas agrupadas por tipo con estado de stock y código de canje (copiable con un clic), los puntos del usuario con actualización casi en tiempo real, y envía notificaciones nativas + Telegram cuando la tienda se rellena. Una sola cuenta y tienda monitoreada a la vez. Se comparte con otros streamers — no es un proyecto 100% personal.

Migrado el 23/08/2026 de Electron/React a **Avalonia (C#)**, la herramienta única de escritorio del stack unificado.

## Cómo se ve y funciona
1. **Setup** (al abrir si no hay streamer/Session-kid guardado): tarjeta con 3 secciones siempre visibles (sin acordeón), cada una con ícono circular numerado — **1. Streamer**, **2. Session-kid** (con ✓ verde cuando tiene valor), **3. Telegram** con su propio recuadro gris claro (ícono de campana + "Notificaciones" + toggle, y si está activo, Bot Token + Chat ID lado a lado + botón "Probar conexión"). Botón azul ancho "▶ Iniciar monitoreo" + texto "🔒 Tus datos están seguros y encriptados" al pie.
2. **Shop**: header con gradiente azul, avatar/nombre/puntos hero + nivel + botón para abrir la tienda del streamer. Botón ↻ junto a los puntos con cooldown 10s. Botón ⏹ Detener en la esquina superior derecha.
3. **Recompensas agrupadas por tipo** (secciones fijas): "Yape / Plin", "Suscripciones", "Recargas", "Otros" — detectado por palabras clave en nombre/código del item. Cada card: imagen | nombre | código de canje (`!code`, clic para copiar) | precio | badge de stock.
4. **Pausa**: al detener muestra pantalla con "Cambiar streamer" o "Reanudar".
5. **Polling tienda**: cada 10-14s (jitter aleatorio) con backoff exponencial hasta 60s en errores consecutivos. **Puntos**: cada 60s + botón manual con cooldown 10s. Barra inferior con texto claro del estado.
6. **Refill detectado**: notificación nativa Windows (balloon tip del ícono de bandeja) + mensaje Telegram + toast dentro de la app.
7. **Tray**: ícono hexágono azul de marca (`NotifyIcon` de WinForms). Al cerrar con X se oculta pero sigue monitoreando. Clic derecho → Salir para cerrar de verdad.
8. **Actualización automática**: al abrir, revisa GitHub Releases (Velopack) — si hay versión nueva, popup "Ahora no" / "Actualizar" (nunca banner). Al actualizar, la propia app se reemplaza y reinicia sola.
9. **Novedades**: popup tipo WhatsApp que se muestra una sola vez después de actualizar — título con la versión + changelog tomado del Release de GitHub. Se descarta con "Entendido", no vuelve a aparecer hasta la próxima versión.

## Stack
- **Avalonia 11 (C#, .NET 8) + CommunityToolkit.Mvvm** — UI multiplataforma, MVVM con source generators (`[ObservableProperty]`/`[RelayCommand]` sobre campos privados — la sintaxis de "partial properties" de C# 13 no la soporta el SDK de .NET 8 instalado).
- **`System.Windows.Forms.NotifyIcon`** (vía `<UseWindowsForms>true</UseWindowsForms>`, `net8.0-windows`) — único uso: ícono de bandeja + notificaciones balloon nativas. La app en sí sigue siendo Avalonia.
- **`HttpClient` nativo** — llamadas REST a botrix.live y Telegram Bot API.
- **`System.Text.Json`** — config persistida en JSON local, en carpeta propia distinta a la versión Electron (a propósito — cada instalación nueva arranca 100% en blanco en el Setup, sin heredar sesión/streamer de nadie, ni siquiera de una instalación Electron previa en la misma PC).
- **Velopack** — auto-actualización vía GitHub Releases, reemplazo atómico del exe.

## Estructura
```
Botrix Refill/
├── PROJECT.md
├── .github/workflows/release.yml   ← build + vpk pack + publish a GitHub Releases (tag v*)
└── project/
    └── BotrixRefill/
        ├── BotrixRefill.csproj
        ├── Program.cs                     ← VelopackApp.Build().Run() primero, luego Avalonia
        ├── App.axaml(.cs)                 ← estilos globales, DataTemplates (ViewLocator)
        ├── app.manifest
        ├── Assets/
        │   ├── icon.png / icon.ico        ← ícono de la app
        │   └── tray.ico                   ← ícono de bandeja (32+16, multi-resolución)
        ├── Models/
        │   ├── AppConfig.cs               ← streamer, sessionKid, telegram*, lastSeenVersion
        │   └── ShopItem.cs                ← ShopItem, BotrixUser, WhoamiResponse
        ├── Services/
        │   ├── ConfigStore.cs             ← load/save JSON en %AppData%\botrix-refill\
        │   ├── BotrixApiService.cs        ← fetch shop items + user (Session-kid header)
        │   ├── TelegramService.cs         ← sendMessage Bot API
        │   ├── PollerService.cs           ← jitter 10-14s + backoff exponencial, eventos ShopUpdated/ItemRefilled
        │   ├── TrayService.cs             ← NotifyIcon: menú, click, balloon tip
        │   ├── UpdateService.cs           ← Velopack: CheckAsync/DownloadAndApplyAsync
        │   └── NewsService.cs             ← compara LastSeenVersion, trae changelog del Release
        ├── Behaviors/
        │   └── WebImageBehavior.cs        ← attached property para cargar imágenes remotas (avatar, recompensas) — Avalonia no soporta Source="url" directo
        ├── Converters/                    ← EqualsConverter, BoolToBrushConverter, ZeroConverter, PollingDotConverter, InitialConverter
        ├── ViewModels/
        │   ├── MainWindowViewModel.cs     ← máquina de estados Setup/Shop/Pausado, tray + refill notification
        │   ├── SetupViewModel.cs
        │   ├── ShopViewModel.cs
        │   ├── RewardCardViewModel.cs     ← badge/color por item + copiar código
        │   ├── RewardGroup.cs / ToastItem.cs
        │   └── ViewModelBase.cs
        └── Views/
            ├── MainWindow.axaml(.cs)      ← titlebar custom, drag, minimize/close, overlay de Pausa
            ├── SetupView.axaml(.cs)
            ├── ShopView.axaml(.cs)
            ├── UpdateAvailableWindow.axaml(.cs)
            └── NewsWindow.axaml(.cs)
```

## Archivos clave
- `Services/PollerService.cs` — misma lógica exacta que el `poller.js` original: jitter 10-14s, backoff exponencial hasta 60s, detección de refill comparando stock anterior vs nuevo por `code`.
- `Behaviors/WebImageBehavior.cs` — Avalonia no tiene equivalente a `<img src="url">`; esta attached property descarga y cachea bitmaps async. Se usa en avatar del usuario e imágenes de recompensas.
- `Services/TrayService.cs` — usa WinForms `NotifyIcon` en vez del `TrayIcon` nativo de Avalonia porque también da `ShowBalloonTip` (notificación nativa) sin depender de registro AppUserModelID, más simple y confiable para un exe portátil sin instalador tradicional.
- `Services/UpdateService.cs` / `NewsService.cs` — `Manager.IsInstalled` evita que el chequeo de updates corra cuando se ejecuta con `dotnet run` (sin metadata de Velopack), solo se activa en el exe empaquetado real.

## Instalar y correr
```bash
cd project/BotrixRefill
dotnet restore     # primera vez
dotnet run          # desarrollo
dotnet build        # solo compilar
```
**Publicar una release nueva:** crear tag `vX.Y.Z` y pushearlo — `.github/workflows/release.yml` compila, empaqueta con `vpk` y publica el GitHub Release solo. También se puede disparar manual (`workflow_dispatch`) indicando la versión.

**Distribución al usuario final:** se comparte el link del Release (`github.com/AndreDiaz11/botrix-refill/releases`), nunca una copia del `.exe` guardada en el proyecto — la carpeta del proyecto solo tiene código fuente, sin builds compilados. El usuario descarga `BotrixRefill-win-Portable.zip` (sin instalador) y corre el `.exe` de adentro.

## Env vars
No requiere ninguna en build/CI (el workflow usa el `GITHUB_TOKEN` automático de Actions). Config de usuario en: `C:\Users\{usuario}\AppData\Roaming\botrix-refill-app\config.json` — carpeta propia, distinta a `botrix-refill` (la de Electron), a propósito: cada instalación nueva arranca sin datos, en el Setup.

## Auto-actualización
Vía GitHub Releases (Velopack), repo: `AndreDiaz11/botrix-refill`. Revisa al abrir, popup "Ahora no"/"Actualizar", reemplazo atómico del exe.

## Novedades para el usuario
Sí — popup tipo WhatsApp una sola vez por versión nueva (proyecto compartido con otros streamers, no 100% personal).

## Despliegue
No aplica en el sentido web — se "despliega" publicando un GitHub Release con tag `vX.Y.Z`. Repo **público** (excepción justificada, mismo criterio que RS Quest Compass Plugin: la app se distribuye a otros streamers y necesita leer GitHub Releases sin token embebido en el cliente; no hay secretos en el repo — Session-kid y Telegram token son config local del usuario, nunca se commitean).

## Claves secretas
Ninguna en el cliente. `GITHUB_TOKEN` del propio workflow de Actions (automático, con permiso `contents: write` solo para ese repo).

## Estado
Funcional: sí | Beta: no (`v1.0.3`, migración cerrada al 100%) | Última revisión: auto-actualización probada de punta a punta con una instalación real (v1.0.0 → detectó v1.0.3 → descargó → se reemplazó → reinició sola en la versión nueva), popup de Novedades verificado con título/versión correctos y sin superposición con el popup de actualización, monitoreo básico de errores (log local) conectado en todos los catch que antes fallaban en silencio, y un crash real encontrado y corregido durante estas pruebas (excepción no manejada en la descarga tumbaba la app).

## Integraciones externas
| Servicio | Endpoint | Auth | Uso |
|---|---|---|---|
| botrix.live shop | `/api/public/shop/items?u={streamer}&platform=kick` | ninguna | items + stock + `code` + `disponibilidad` |
| botrix.live user | `/api/public/leaderboard/whoamiKick?user={streamer}&t={ts}` | Header `Session-kid` | puntos + nivel + avatar |
| Telegram Bot API | `POST /bot{token}/sendMessage` | token en URL | notificación de refill + prueba de conexión |
| GitHub Releases API | `/repos/AndreDiaz11/botrix-refill/releases/latest` | ninguna (repo público) | changelog para el popup de Novedades |
| GitHub Releases (Velopack) | vía `GithubSource` | ninguna (repo público) | auto-actualización del exe |

**Session-kid**: F12 → Application → Local Storage en botrix.live.

**Agrupación de recompensas**: sin categoría real en la API, se detecta por palabras clave en `code`+`name`: "yape"/"plin" → *Yape / Plin*; "sub" → *Suscripciones*; "bet"/"recarga" → *Recargas*; el resto → *Otros* (`ShopViewModel.GroupDefs`).

## Escalabilidad
- Una cuenta/tienda a la vez. Para múltiples: array de configs + múltiples `PollerService` + tabs en UI.
- Cambiar rango de polling: constantes `MinDelayMs`/`MaxDelayMs`/`MaxBackoffMs` en `PollerService.cs`.
- Nuevo grupo de recompensas: `ShopViewModel.GroupDefs`.
- Multiplataforma (Mac/Linux): Avalonia ya lo permite en la UI; `TrayService` (WinForms) y el publish `win-x64` de Velopack son Windows-only — habría que condicionar por plataforma si se pide a futuro.

## Compatibilidad
Solo Windows x64. Publicado self-contained (no requiere .NET instalado en la PC del usuario) vía Velopack.

## Datos de prueba
No aplica — consulta la cuenta real de botrix.live/Kick que el usuario configura en Setup.

## Versión
1.1.0 — rediseño visual del Setup a pedido explícito (referencia dada por el usuario): sin acordeón, secciones numeradas con ícono, recuadro propio para Telegram, casilla de verificación en Session-kid.

## Cambios
1. (23/08/2026) Rediseño completo de `SetupView.axaml`: acordeón eliminado, las 3 secciones (Streamer/Session-kid/Telegram) quedan siempre visibles con ícono circular numerado (usuario/llave/avión de papel dibujados con `Path`/`Line`/`Polygon`, sin necesitar una librería de íconos). Session-kid muestra un ✓ verde cuando tiene valor. Telegram vive en su propio recuadro gris claro con ícono de campana + toggle "Notificaciones", y Bot Token/Chat ID en dos columnas. Verificado visualmente contra la referencia dada por el usuario — coincide.
3. (23/08/2026) `ConfigStore` pasado a `%AppData%\botrix-refill-app\config.json` (antes reutilizaba la carpeta `botrix-refill` de la versión Electron por continuidad). A pedido explícito: cada usuario que descarga la app debe empezar en cero en el Setup, sin heredar ninguna sesión previa, ni siquiera si tuvo la versión Electron instalada antes en la misma PC.
3. (23/08/2026) Fix crítico: `UpdateAvailableWindow.UpdateClick` era `async void` sin try/catch — cualquier falla real durante la descarga (ej. lock de Velopack ocupado, sin conexión) tumbaba toda la app sin dejar rastro. Ahora atrapa la excepción, la registra en el log local y muestra un mensaje de error sin cerrar la app. Agregada red de seguridad global (`AppDomain.UnhandledException` + `TaskScheduler.UnobservedTaskException`) para que cualquier excepción no anticipada quede en el log en vez de perderse.
4. (23/08/2026) Fix de auto-update/Novedades encontrado probando contra una instalación real: los popups de Novedades y Actualización disponible se mostraban superpuestos (`CheckNewsAsync`/`CheckUpdatesAsync` corrían en paralelo) — ahora Novedades se muestra primero y se espera a que se cierre antes de chequear actualizaciones. Además, `NewsService` traía el changelog de `releases/latest` (la versión más nueva en GitHub) en vez del Release que coincide con la versión que realmente está corriendo — ahora consulta `releases/tags/v{versión actual}`.
5. (23/08/2026) Monitoreo de errores: `Services/ErrorLogger.cs` — log local (`%AppData%\botrix-refill-app\error-log.txt`, tope de 500 líneas) conectado en los catch que antes fallaban en silencio (poller, refresh de puntos, Telegram, guardado de config). Sin Supabase porque el proyecto no tiene backend propio — todo el tráfico va directo del cliente a APIs públicas.
6. (23/08/2026) Migración completa de Electron/React a Avalonia (C#): mismas 3 pantallas (Setup/Shop/Pausa), mismo polling con jitter+backoff, mismas notificaciones nativas + Telegram, mismo tray, misma paleta azul. Repo git creado desde cero (el proyecto no tenía control de versiones), GitHub Releases + Velopack para auto-actualización real, popup de Novedades (proyecto compartido con otros streamers). Verificado visualmente con datos reales de producción.
