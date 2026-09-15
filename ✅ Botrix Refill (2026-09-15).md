# Botrix Refill

## Qué es
App de escritorio Windows que monitorea la tienda de puntos de un streamer en botrix.live. Muestra las recompensas agrupadas por tipo con estado de stock y código de canje (copiable con un clic), los puntos del usuario con actualización casi en tiempo real, y envía notificaciones nativas + Telegram cuando la tienda se rellena. Una sola cuenta y tienda monitoreada a la vez. Se comparte con otros streamers — no es un proyecto 100% personal.

Migrado el 23/08/2026 de Electron/React a **Avalonia (C#)**, la herramienta única de escritorio del stack unificado.

## Cómo se ve y funciona
1. **Setup** (al abrir si no hay streamer/Session-kid guardado): tarjeta con 3 secciones siempre visibles (sin acordeón), cada una con ícono circular numerado — **1. Streamer**, **2. Session-kid** (con ✓ verde cuando tiene valor, y botón "?" con un popup de ayuda paso a paso para sacarlo de botrix.live), **3. Telegram** con su propio recuadro gris claro (ícono de campana + "Notificaciones" + toggle, y si está activo, Bot Token + Chat ID lado a lado + botón "Probar conexión"). Botón azul ancho "▶ Iniciar monitoreo" + texto "🔒 Tus datos se guardan solo en esta computadora" al pie.
2. **Shop**: header en una sola línea con gradiente azul-índigo — avatar + nombre + puntos agrupados a la izquierda (con "Revisado en: {streamer}" como caption debajo, en vez de repetir el streamer en la barra de título de la ventana), y a la derecha, separado, 🕐 última revisión + botón ⏹ Detener. Sin las tarjetas de estadísticas de recompensas disponibles/reclamadas (se sacaron a pedido del usuario, no aportaban valor) ni el banner informativo fijo (también sacado).
3. **Recompensas agrupadas por tipo, en tabla** (no tarjetas): "Yape / Plin", "Suscripciones", "Recargas", "Otros" — cada grupo con ícono de color propio + una tabla (Recompensa / Puntos / Disponibilidad / Acción). Acción es un botón azul "🎁 Canjear" siempre activo (copia `!code` al portapapeles), sin importar el estado de stock mostrado — a pedido del usuario, porque la web tarda en reflejar el stock nuevo.
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
        ├── Converters/                    ← BoolToBrushConverter, ZeroConverter, PollingDotConverter, InitialConverter
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
Funcional: sí | Beta: no (`v1.5.2`) | Última revisión (15/09/2026): usuario confirmó, probando v1.5.2 en real, que el diseño quedó bien ("para mí está perfecto ahora sí"). Con esto se cierra la ronda de feedback iniciada tras la revisión inicial del proyecto — sin pendientes abiertos reportados por el usuario.

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
1.5.2 — el fix de v1.5.1 (columna de ancho fijo + espaciador al final) resultó igual de feo, solo movió el hueco vacío del medio de la fila al final — corregido de raíz: se quitó el espaciador y se achicó el ancho máximo de la tarjeta para que calce justo con el contenido de la tabla, sin dejar hueco ni adentro ni afuera. También se rehicieron el ícono de campana (con formas separadas — polígono + círculo — en vez de un solo trazo compuesto que aparentemente seguía sin verse bien) y el botón de refrescar puntos (el símbolo de flecha circular de texto, reemplazado por un ícono dibujado con las mismas formas simples).

## Cambios
1. (15/09/2026) Usuario confirmó v1.5.2 como versión final de esta ronda ("para mí está perfecto ahora sí"). Auditoría de cierre sobre todo el código tocado en la sesión (revisión de `.cs`/`.axaml` completa, sin ejecutar build): sin referencias rotas ni código muerto de lo eliminado (`AvailableCount`, `RedeemedTodayCount`, `RedemptionTracker`, `TitlebarPill`, evento `Redeemed`), sin bindings XAML apuntando a propiedades inexistentes, sin `Grid.Column` fuera de rango. Único hallazgo, cosmético: `using Avalonia.Input.Platform;` sin usar en `RewardCardViewModel.cs` — eliminado. Verificado con `dotnet build` sin errores.
2. (15/09/2026) 3 ajustes más sobre v1.5.1, con el usuario pidiendo explícitamente revisar el plan antes de tocar código: **espacio vacío al final de la tabla** — el fix de v1.5.1 (columna `*` al final para absorber el espacio sobrante) no eliminó el hueco, solo lo movió del medio de la fila al final — igual de feo. Corregido de raíz: se sacó esa columna extra (`ColumnDefinitions="300,110,150,140"`, sin `*`) y se achicó el `MaxWidth` del cuerpo del Shop de 1080 a 760px — ese ancho calza casi exacto con el contenido real de la tabla (700px de columnas + 32px de márgenes internos de la tarjeta), así que ya no queda hueco ni en el medio ni al final; si la ventana es más ancha, el espacio de más queda AFUERA de la tarjeta (margen de página normal), no adentro. **Ícono de Notificaciones, otra vuelta**: el ícono con un `Path` de un solo trazo compuesto (dos subpaths combinados) aparentemente seguía sin verse bien pese a usar coordenadas de una librería conocida — se rehizo con 2 elementos separados (`Polygon` para el cuerpo de la campana + `Ellipse` para el badajo), el mismo patrón que ya funciona sin problemas en los íconos de "1. Streamer" y "2. Session-kid" (formas simples independientes, no un trazo compuesto). **Ícono de refrescar puntos**: el usuario aclaró que el problema del punto 3 no era el hexágono de puntos sino la flecha circular de texto (↻) del botón de refrescar, al lado de "pts" — se reemplazó por un ícono dibujado (arco + triángulo), mismo criterio de formas simples.
3. (15/09/2026) 3 ajustes más sobre v1.5.0, tras feedback del usuario: **espacio excesivo entre recompensa y puntos** — en `ShopView.axaml`, la tabla usaba `ColumnDefinitions="*,120,160,150"` (columna del nombre flexible, ocupando todo el ancho sobrante) — cambiado a `"300,110,150,140,*"` (nombre con ancho fijo de 300px, con recorte de texto `TextTrimming="CharacterEllipsis"` si es muy largo, y la columna flexible movida al FINAL de la fila) — así Puntos/Disponibilidad/Acción quedan siempre pegados al nombre, sin importar qué tan ancha esté la ventana. **Ícono de Notificaciones no combinaba con la paleta** — el emoji 🔔 puesto en v1.5.0 (reemplazo de emergencia del ícono roto) se ve a color (dorado/amarillo), mientras que los otros 2 íconos del Setup (Streamer, Session-kid) son trazos azules sobre un círculo celeste claro — se reemplazó por un ícono de campana real con un `Path` de coordenadas confiables (tomado de Feather Icons, una librería de íconos ampliamente usada, no inventado a mano como el original roto), en el mismo estilo de trazo azul que los demás, con fondo `#EFF6FF` en vez del círculo azul sólido que tenía antes. **Botón "?" de ayuda desalineado** — pequeño ajuste de margen y de alineación de contenido para que quede a la misma altura que el texto "2. Session-kid". Corregido en el camino: un error de compilación (`StrokeLineJoin` no es una propiedad válida en esta versión de Avalonia, es `StrokeJoin`) detectado al compilar antes de publicar — nunca llegó a subirse una versión rota.
4. (15/09/2026) **Limpieza visual del Shop y del Setup, a pedido explícito del usuario** tras un pase completo probando la app: (1) sacadas las 2 tarjetas de estadísticas del header ("🎁 Recompensas disponibles" y "⚡ Recompensas reclamadas hoy") — el usuario dijo que no las necesita, así que se eliminó también todo el código que las sostenía: `Services/RedemptionTracker.cs` (archivo completo, borrado), los campos `RedeemedTodayDate`/`RedeemedTodayCount` de `AppConfig`, el evento `Redeemed` de `RewardCardViewModel`, y las propiedades `AvailableCount`/`RedeemedTodayCount` de `ShopViewModel`. (2) El nombre del streamer ya no se repite en la barra de título de la ventana (`MainWindowViewModel.TitlebarPill`, eliminado por completo) — en el header del Shop ahora dice "Revisado en: {streamer}" como caption debajo del nombre, en vez del punto verde + nombre repetido. (3) Nombre de usuario y puntos ahora van agrupados juntos a la izquierda del header; hora de última revisión y botón Detener van agrupados a la derecha, separados por el espacio flexible del medio. (4) Sacado el banner informativo "Canjea tus puntos... El stock se actualiza automáticamente" — el usuario lo consideró irrelevante — y reducido el padding superior del cuerpo, para achicar el espacio vacío entre el header y la primera recompensa. (5) Sacadas las píldoras verde/gris del pie de la tienda ("X recompensas disponibles" / "X recompensas en total") — no aportaban nada según el usuario. (6) El ícono de campana de "Notificaciones" en el Setup, dibujado a mano con un `Path` de coordenadas SVG poco fiable, se reemplazó por el emoji 🔔 — es la sospecha más probable detrás del "diseño feo/mal hecho en las esquinas" que reportó el usuario en una captura (una forma blanca con un doblez, no una campana reconocible). Verificado con `dotnet build` sin errores; **sin poder confirmar visualmente desde acá, a la espera de que el usuario pruebe esta versión**.
5. (15/09/2026) **Causa real del bug de instancias duplicadas, encontrada por el log de diagnóstico de v1.4.1**: el usuario compartió el contenido de `error-log.txt` y ahí quedó claro que nunca hubo una segunda instancia real — el log solo mostraba líneas de "Instancia nueva" y "Escuchando pedidos" del MISMO proceso, repetidas cada vez que se reabría la ventana desde el tray. La causa real: `Window.Opened` de Avalonia se dispara de nuevo cada vez que la ventana pasa de oculta a visible (`Show()` después de `Hide()`), no solo la primera vez como se asumió al escribir `MainWindow.OnOpened` originalmente. Cada vez que el usuario minimizaba al tray y volvía a abrir, `OnOpened` volvía a correr entero: creaba un `TrayService` nuevo sin eliminar el anterior (de ahí los varios íconos hexagonales acumulados en la bandeja, vistos en capturas anteriores del usuario — nunca fueron procesos distintos, eran íconos de bandeja filtrados del mismo proceso) y volvía a llamar `CheckNewsAndUpdatesAsync()`, mostrando el popup de Novedades otra vez como si fuera la primera apertura. También explica una excepción que aparecía en el log ("Cannot show window with non-visible owner"): a veces `OnOpened` corría mientras la ventana todavía no terminaba de mostrarse, y el popup de Novedades intentaba abrirse con dueño no visible. Fix: agregada una bandera `_initialized` al principio de `OnOpened` que hace que toda esa inicialización corra una sola vez por proceso, sin importar cuántas veces se dispare el evento. El candado de instancia única (`Mutex`/`Global\`) agregado en v1.3.0/reforzado en v1.4.1 se mantiene como red de seguridad extra para el caso real de dos procesos separados, pero no era la causa de este bug en particular.
6. (15/09/2026) Refuerzo del candado de instancia única — el usuario confirmó que v1.4.0 (header en una línea, tope de ancho, scroll, ayuda de Session-kid) quedó bien, pero seguía reportando que reabrir la app desde el ícono de la barra de tareas abría una instancia nueva y repetía el popup de Novedades como si fuera la primera vez, a pesar del candado agregado en v1.3.0/reforzado en v1.4.0. Sin acceso directo a la PC del usuario para reproducirlo, se hicieron 2 cosas: (1) el `Mutex` y el `EventWaitHandle` de `Program.cs`/`MainWindow.axaml.cs` ahora usan el prefijo `Global\` en vez de nombre local — evita que el candado falle en silencio si las dos instancias corrieran en contextos de sesión/privilegios distintos —, y se ensanchó el manejo de errores alrededor de `EventWaitHandle.OpenExisting` (antes solo atrapaba `WaitHandleCannotBeOpenedException`; ahora cualquier excepción cae al mismo aviso, nunca deja pasar una ventana nueva por un error inesperado). (2) Se agregó `ErrorLogger.LogInfo` y llamadas de diagnóstico en cada rama de la lógica de instancia única (instancia nueva creada / instancia existente detectada / señal enviada / señal recibida) — si el problema persiste, revisar `%AppData%\botrix-refill-app\error-log.txt` va a decir exactamente en qué rama falla, en vez de tener que adivinar. **Pendiente de confirmación del usuario** tras limpiar procesos viejos y probar de nuevo.
7. (15/09/2026) 4 correcciones más, tras feedback del usuario probando v1.3.0 en real:
   - **Contenido se estiraba al maximizar la ventana**: la tabla de recompensas y el header no tenían límite de ancho, así que en una ventana grande/maximizada todo se veía exageradamente estirado. Se agregó `MaxWidth="1080"` centrado tanto al header como al cuerpo de `ShopView.axaml` — a partir de esa medida, el contenido queda fijo y centrado en vez de seguir creciendo.
   - **Scroll no mostraba el último grupo completo**: se agregó un espaciador al final de la lista de recompensas para que el último grupo (ej. "Recargas") quede totalmente visible al hacer scroll hasta abajo, en vez de quedar cortado justo antes de la barra de estado.
   - **Popup de ayuda para el Session-kid**: agregado un botón "?" junto al título "2. Session-kid" en el Setup, con los pasos exactos para sacarlo de botrix.live (F12 → Application → Local Storage) y un aviso de que este valor vence con el tiempo y hay que volver a copiarlo si la app deja de leer los puntos.
   - **Instancias duplicadas / popup de actualización repetido, reforzado**: el candado de instancia única de v1.3.0 solo evita que se abran copias NUEVAS — no cierra instancias viejas que ya estuvieran corriendo desde antes de ese fix (quedan como procesos sueltos, cada una con su propio ícono en la bandeja). Ahora además, al intentar abrir una segunda copia, en vez de solo mostrar un aviso, la instancia real que ya está corriendo se trae al frente automáticamente (`EventWaitHandle` con nombre fijo, escuchado en un hilo de fondo de `MainWindow`). **Importante para el usuario:** hay que cerrar TODAS las copias viejas que quedaron abiertas antes de este fix (Administrador de tareas → buscar "BotrixRefill.exe" → Finalizar tarea en cada una, o simplemente reiniciar la PC) — de ahí en adelante, con una sola instancia corriendo, no debería volver a duplicarse.
8. (15/09/2026) Corrección al cache de NuGet agregado unas horas antes (ver entrada 2 más abajo): medido en un release real, no aportaba ninguna mejora — el `dotnet publish` self-contained tarda unos 32s en total y de eso solo 14s son restauración (ya era rápido sin cache), y como el workflow solo corre al publicar un tag nuevo (no en cada push a `main`), cada tag queda en un scope de cache aislado en GitHub Actions y nunca llega a reusar el cache del tag anterior — confirmado en el log del release v1.3.0 ("Cache not found for input keys"). El cache solo sumaba ~7s de más subiendo un archivo de 485MB que nunca se aprovechaba. Se quitó el paso de `.github/workflows/release.yml` — el pipeline real (arranque del runner + `dotnet publish` + `vpk pack` + subida a GitHub Releases) ya está en ~2 minutos, que es básicamente el piso dado el tiempo fijo de aprovisionar el runner y subir los archivos del release.
9. (15/09/2026) 4 fixes a pedido del usuario tras usar la app en real:
   - **Puntos no cargaban**: `BotrixApiService.FetchUserAsync` ahora lanza un error explícito ("Tu Session-kid parece inválido o vencido") cuando `botrix.live` responde 200 OK pero sin el objeto `user` (esto pasaba en silencio antes — la API no devuelve error HTTP, solo omite los datos, así que la app se quedaba con los puntos en blanco sin avisar nada). Se probó el endpoint directo con `curl`: con o sin `Session-kid`, responde igual sin `user` — confirma que la causa real es que el token que tiene guardado el usuario no es válido/vigente, no un bug de la app en sí. Se agregó `ShopViewModel.PointsErrorMessage`, mostrado en rojo debajo de los puntos en el header, y se separó la carga de tienda y de puntos en tareas independientes (`LoadShopAsync`/`RefreshUserAsync`) para que un fallo en puntos ya no bloquee la carga de la tienda. También se recorta el Session-kid de comillas sueltas al guardar (error común al copiar desde DevTools).
   - **Botón "Canjear" siempre activo**: a pedido explícito (la web tarda ~1 min en reflejar el stock nuevo y el usuario pierde recompensas por eso) — se quitó el bloqueo "🔒 Sin stock" de la columna Acción en `ShopView.axaml`; el botón de copiar código está disponible siempre, sin importar el estado de stock mostrado. La columna "Disponibilidad" sigue mostrando el estado real como referencia visual.
   - **Instancias duplicadas + popup de actualización repetido**: la app no tenía candado de instancia única — cada vez que se volvía a abrir el `.exe` (en vez de usar el ícono de la bandeja) se creaba un proceso nuevo, cada uno con su propia ventana en la barra de tareas, y cada uno repitiendo el chequeo de Novedades/Actualización. Agregado `Mutex` con nombre fijo en `Program.cs`: si ya hay una instancia corriendo, la nueva muestra un aviso ("Botrix Refill ya está abierto, revisa la bandeja") y se cierra sin abrir una segunda ventana.
   - **Rediseño del header del Shop**: gradiente más profundo (azul-índigo en diagonal) con 2 círculos difuminados de fondo para dar profundidad, sombra debajo del header separándolo del cuerpo, avatar con anillo, puntos dentro de una píldora propia, tarjetas de estadísticas con ícono en círculo y borde sutil, botón Detener con estado hover. **Nota:** no se pudo verificar visualmente en una ventana real (sin herramienta para capturar pantallas de apps de escritorio nativas) — se verificó que compila, pero conviene que el usuario confirme cómo se ve.
10. (23/08/2026) Revisión completa del proyecto a pedido del usuario (código, estructura, pipeline de release) — sin errores críticos encontrados. Corregido: `SetupView.axaml` decía "Tus datos están seguros y encriptados" pero el archivo de configuración (`ConfigStore.cs`) guarda el Session-kid y el token de Telegram en texto plano sin cifrar — se cambió el texto a "Tus datos se guardan solo en esta computadora" (afirmación real, sin agregar cifrado por ser dato local de un solo usuario). `.github/workflows/release.yml`: agregado cache de paquetes NuGet (revertido al día siguiente, ver entrada 1 más arriba — no funcionaba como esperado). Código muerto eliminado: `Converters/EqualsConverter.cs` (sin ninguna referencia en el proyecto), estilos `Button.accordion-head` y `Border.badge` en `App.axaml` (sobras del acordeón y las tarjetas viejas del Setup/Shop, ya reemplazados), y la copia redundante de `Assets/icon.png` al build output en el `.csproj` (el ícono ya se embebe vía `AvaloniaResource`, ese `None Include` no se usaba). Verificado con `dotnet build` sin errores tras cada cambio.
11. (23/08/2026) Rediseño completo de `ShopView.axaml`: recompensas ahora en tabla (Recompensa/Puntos/Disponibilidad/Acción) agrupada por tipo con ícono de color propio, en vez de tarjetas con imagen. El código de canje pasa a ser un botón azul "🎁 Canjear" (antes era un chip con el texto `!code`) — al hacer clic copia el mismo código al portapapeles que antes. Header reestructurado en 2 filas: la superior (avatar/puntos + botón Detener) y la inferior (3 tarjetas: recompensas disponibles, reclamadas hoy, última revisión) — el botón Detener queda fijo en su propia fila y ya no puede superponerse con nada. Agregado `Services/RedemptionTracker.cs`: contador de recompensas reclamadas persistido por día (`AppConfig.RedeemedTodayCount`/`RedeemedTodayDate`), se resetea solo al cambiar de fecha. Verificado visualmente contra la referencia dada por el usuario — coincide.
12. (23/08/2026) `MainWindow.CheckNewsAndUpdatesAsync`: las consultas de red de Novedades y Actualización disponible ahora se disparan en paralelo (`Task.WhenAll`) en vez de una después de la otra — el popup de Actualización aparece bastante más rápido al abrir. El orden en que se MUESTRAN (Novedades primero si aplica, luego Actualización) no cambia, solo el orden en que se consultan.
13. (23/08/2026) Rediseño completo de `SetupView.axaml`: acordeón eliminado, las 3 secciones (Streamer/Session-kid/Telegram) quedan siempre visibles con ícono circular numerado (usuario/llave/avión de papel dibujados con `Path`/`Line`/`Polygon`, sin necesitar una librería de íconos). Session-kid muestra un ✓ verde cuando tiene valor. Telegram vive en su propio recuadro gris claro con ícono de campana + toggle "Notificaciones", y Bot Token/Chat ID en dos columnas. Verificado visualmente contra la referencia dada por el usuario — coincide.
14. (23/08/2026) `ConfigStore` pasado a `%AppData%\botrix-refill-app\config.json` (antes reutilizaba la carpeta `botrix-refill` de la versión Electron por continuidad). A pedido explícito: cada usuario que descarga la app debe empezar en cero en el Setup, sin heredar ninguna sesión previa, ni siquiera si tuvo la versión Electron instalada antes en la misma PC.
15. (23/08/2026) Fix crítico: `UpdateAvailableWindow.UpdateClick` era `async void` sin try/catch — cualquier falla real durante la descarga (ej. lock de Velopack ocupado, sin conexión) tumbaba toda la app sin dejar rastro. Ahora atrapa la excepción, la registra en el log local y muestra un mensaje de error sin cerrar la app. Agregada red de seguridad global (`AppDomain.UnhandledException` + `TaskScheduler.UnobservedTaskException`) para que cualquier excepción no anticipada quede en el log en vez de perderse.
16. (23/08/2026) Fix de auto-update/Novedades encontrado probando contra una instalación real: los popups de Novedades y Actualización disponible se mostraban superpuestos (`CheckNewsAsync`/`CheckUpdatesAsync` corrían en paralelo) — ahora Novedades se muestra primero y se espera a que se cierre antes de chequear actualizaciones. Además, `NewsService` traía el changelog de `releases/latest` (la versión más nueva en GitHub) en vez del Release que coincide con la versión que realmente está corriendo — ahora consulta `releases/tags/v{versión actual}`.
17. (23/08/2026) Monitoreo de errores: `Services/ErrorLogger.cs` — log local (`%AppData%\botrix-refill-app\error-log.txt`, tope de 500 líneas) conectado en los catch que antes fallaban en silencio (poller, refresh de puntos, Telegram, guardado de config). Sin Supabase porque el proyecto no tiene backend propio — todo el tráfico va directo del cliente a APIs públicas.
18. (23/08/2026) Migración completa de Electron/React a Avalonia (C#): mismas 3 pantallas (Setup/Shop/Pausa), mismo polling con jitter+backoff, mismas notificaciones nativas + Telegram, mismo tray, misma paleta azul. Repo git creado desde cero (el proyecto no tenía control de versiones), GitHub Releases + Velopack para auto-actualización real, popup de Novedades (proyecto compartido con otros streamers). Verificado visualmente con datos reales de producción.
