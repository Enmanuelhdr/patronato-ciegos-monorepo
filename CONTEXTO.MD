# Contexto de la sesión — Patronato Nacional de Ciegos (monorepo)

> Documento generado para dejar registro de todo lo trabajado en esta sesión: de dónde se partió, qué se decidió, qué se construyó, qué se probó y qué queda pendiente. Pensado para que cualquiera (incluido un Claude futuro sin memoria de esta conversación) pueda retomar el trabajo sin perder contexto.

**Estado al cierre de la sesión:** todos los cambios descritos aquí están **en `git add` (staged) pero SIN COMMITEAR**, por decisión explícita del usuario. Antes de seguir trabajando, correr `git status` para confirmar que sigue así.

---

## 1. Punto de partida

La sesión arrancó con una rama `main` en medio de un **merge sin terminar** (`git merge` en curso, conflictos en `ApplicationDbContext.cs`, `Program.cs` y los archivos binarios de SQLite). El objetivo inicial era resolver esos conflictos e integrar una app de Caja (WPF) que un compañero había agregado, haciendo que pasara por el API Gateway en vez de hablarle directo al Core.

A partir de ahí, la sesión fue creciendo en fases (cada una pedida explícitamente por el usuario):

1. Resolver el merge e integrar Caja ↔ Gateway ↔ Core.
2. Auditar los endpoints reales del Core y recortar la Caja a "solo las pantallas justas".
3. Hacer que la Caja use **explícitamente sockets** (no HTTP) contra el Gateway, como un cajero automático.
4. Agregar **resiliencia offline** en dos niveles: Caja→Gateway y Gateway→Core (store-and-forward).
5. Construir el proyecto **Web en React** (con pnpm) que consume el Gateway.
6. Agregarle una **landing page pública** a la Web (no solo login).

---

## 2. Arquitectura resultante

```
Patronato.Caja (WPF, terminal de cajero)
   │  SOLO SOCKET TCP (puerto 6000) — sin HTTP, sin base de datos propia
   ▼
Patronato.Gateway (ASP.NET Core)
   │  HTTP interno (agrega X-Gateway-Key) + reverse-proxy transparente
   ▼
Patronato.Core (ASP.NET Core + EF Core + SQLite "patronato.db")

Patronato.Web (React + Vite + pnpm, navegador)
   │  HTTP (fetch) directo al Gateway, nunca al Core
   ▼
Patronato.Gateway  →  Patronato.Core
```

**Regla de oro de todo el sistema:** nadie (ni la Caja ni la Web) le habla directo al Core. El Core tiene un middleware que **rechaza con 401** cualquier request a `/api/*` que no traiga el header `X-Gateway-Key` (que solo el Gateway conoce y agrega automáticamente).

### Puertos (perfiles de desarrollo)

| Proyecto | HTTP | HTTPS | Otro |
|---|---|---|---|
| `Patronato.Core` | 57141 | 57140 | — |
| `Patronato.Gateway` | 57171 | 57170 | socket TCP **6000** (Caja) |
| `Patronato.Web` | 5173 (Vite dev) | — | — |

### Cómo levantar todo para probar

```bash
# 1. Core
cd Patronato.Core && dotnet run --urls http://localhost:57141

# 2. Gateway (necesita el Core arriba para las consultas/cobros en vivo,
#    pero puede arrancar solo y encola lo que no pueda enviar)
cd Patronato.Gateway && dotnet run --urls http://localhost:57171

# 3a. Caja (WPF, requiere Windows con sesión de escritorio)
cd Patronato.Caja && dotnet run

# 3b. Web (React)
cd Patronato.Web && pnpm install && pnpm dev   # http://localhost:5173
```

Usuarios semilla (`DbInitializer.cs`): `admin/admin123` (Administrador), `cajero/cajero123` (Cajero), `recepcion/recepcion123` (Recepcionista).

---

## 3. Fase 1 — Merge y primera integración Caja↔Gateway↔Core

### Conflictos resueltos
- **`Patronato.Core/Data/ApplicationDbContext.cs`**: se conservaron ambos lados — `DbSet<LogEntry> Logs` (logging) y `DbSet<TransaccionCaja>` / `DbSet<OrdenPedidoCaja>` (negocio de Caja). No eran excluyentes.
- **`Patronato.Core/Program.cs`**: se conservó la creación de la tabla `Logs` (log4net) **y** la llamada a `DbInitializer.Initialize(db)` (datos semilla). Tampoco eran excluyentes.
- **`patronato.db` / `.db-shm` / `.db-wal`** (en raíz y en `Patronato.Core/`): binarios de SQLite con journal WAL, imposibles de fusionar como texto. Se hizo `git rm --cached` y se agregaron `*.db`, `*.db-shm`, `*.db-wal` al `.gitignore` — la base se regenera sola al arrancar el Core (`EnsureCreated` + `DbInitializer`).

### Gap descubierto y corregido en el Gateway
El Gateway ya tenía un esquema de "códigos de transacción" (inspirado en ISO 8583: `Mti`, `Stan`, `CodigoTransaccion`, `Datos` → `CodigoRespuesta` 00/05/12/96) usado tanto por la Web (REST `/api/gateway/transaccion`) como por la Caja (socket). Pero los 4 códigos de cobro que ya existían (`MASAJE_COBRAR`, `PRESTAMO_ABONAR`, `PRODUCTO_VENDER`, `DONACION_REGISTRAR`) **descartaban** el cajero, la sucursal, el método de pago y el monto recibido reales — se quedaban con los valores por defecto del Core. Se corrigió (`ConstruirQueryCobro` en `CoreGatewayService.cs`) para que viajen completos. Se agregó el código que faltaba: `VENTA_CARRITO` (la Caja vende por carrito, no por producto individual).

### Primera versión de la integración
En esa primera pasada, la Caja usaba:
- **Socket** (el esquema de códigos) solo para los 4 cobros.
- **HTTP contra el Gateway** (que lo reenvía al Core) para todo lo demás: login, catálogo, listados.

Esto cambió en la Fase 3 (ver abajo): ahora la Caja usa **solo socket** para absolutamente todo.

---

## 4. Fase 2 — Auditoría de endpoints y recorte de la Caja

El usuario notó que la Caja tenía pantallas que, según los propios comentarios del `CajaController.cs` del Core, **no son tareas de Caja sino de Recepción/Admin**. Se hizo un recorte function-by-function, confirmado explícitamente con el usuario antes de aplicarlo.

### Se quedó en la Caja (cobro real — lo que el Core marca como función de cajero)
- Login (restringido a roles **Cajero** y **Administrador** — se quitó Recepcionista)
- Venta POS (carrito) + "Cargar Orden" pendiente (cobra una orden que alguien más generó)
- Masajes TACTO → cobrar
- Préstamos → aplicar abono
- Donaciones (el propio código decía: "el recibo oficial de donaciones se emite en Caja")
- Arqueo de Caja
- Historial de Recibos

### Se eliminó de la Caja (son tareas de Recepción/Admin, aunque el Core las soporte)
| Qué se borró | Por qué |
|---|---|
| Ventana "Agendar Masaje" (`AgendarMasajeWindow.*`) | Agendar es tarea de Recepción |
| Ventana "Otorgar Préstamo" (`OtorgarPrestamoWindow.*`) | Otorgar crédito es decisión de comité/Recepción; Caja solo cobra cuotas ya aprobadas |
| Botón/flujo "Generar Orden" (`CrearOrdenAsync`, `CrearOrdenDto`) | Crear la cotización es de Recepción; cobrarla (cargarla) sí se quedó |
| Pestaña "Gestión de Cajeros" (`GetUsuariosAsync`, `CrearUsuarioAsync`, `UsuarioDto`) | Alta de empleados es RRHH/Web, no función de terminal de cobro |
| Rol Recepcionista completo en la Caja | Sin esas pantallas, no tenía nada que hacer ahí |

### Corrección de lógica adicional
`ApiService.LoginAsync` hardcodeaba los 3 nombres de sucursal en un `switch` en vez de preguntarle al Core. Se cambió para consultar `/api/Sucursales` de verdad (hoy, código `SUCURSALES_CONSULTAR` por socket).

**Todo lo que se quitó de la Caja sigue existiendo en el Core** (los endpoints de `CajaController`, `UsuariosController`, etc. no se tocaron) — simplemente la Caja ya no los llama. Esas funciones reaparecieron después como pantallas de **Patronato.Web** (ver Fase 5).

---

## 5. Fase 3 — La Caja habla SOLO por socket (como un cajero automático)

Pedido explícito: "el api gateway tiene las dos formas pero la caja debe usar explícitamente los sockets".

Se expandió el esquema de códigos del Gateway para que cubra **todo** lo que la Caja necesita, no solo los cobros. `Patronato.Caja.Services.ApiService` quedó **sin ningún `HttpClient`** — se eliminó por completo.

### Los 15 códigos de transacción (socket, puerto 6000)

| Código | Qué hace en el Core | Tipo |
|---|---|---|
| `PING` | Nada — línea viva, no toca el Core | Conectividad |
| `LOGIN` | `POST /api/Usuarios/login` | Consulta |
| `SUCURSALES_CONSULTAR` | `GET /api/Sucursales` | Consulta |
| `INVENTARIO_CONSULTAR` | `GET /api/inventario` | Consulta |
| `MASAJES_PENDIENTES_CONSULTAR` | `GET /api/caja/masajes-pendientes` | Consulta |
| `CLIENTES_FRECUENTES_CONSULTAR` | `GET /api/caja/clientes-frecuentes` | Consulta |
| `ORDENES_PENDIENTES_CONSULTAR` | `GET /api/caja/ordenes-pendientes` | Consulta |
| `PRESTAMOS_ACTIVOS_CONSULTAR` | `GET /api/caja/prestamos-activos?filtro=` | Consulta |
| `CUADRE_CAJA_CONSULTAR` | `GET /api/caja/cuadre-caja?fecha=&cajero=` | Consulta |
| `TRANSACCIONES_CONSULTAR` | `GET /api/caja/transacciones?fecha=&cajero=` | Consulta |
| `MASAJE_COBRAR` | `POST /api/caja/cobrar-masaje/{id}` | **Cobro** |
| `PRESTAMO_ABONAR` | `POST /api/caja/abonar-prestamo` | **Cobro** |
| `PRODUCTO_VENDER` | `POST /api/caja/vender-producto` | **Cobro** (no usado por la UI actual, queda disponible) |
| `VENTA_CARRITO` | `POST /api/caja/vender-carrito` | **Cobro** |
| `DONACION_REGISTRAR` | `POST /api/caja/donacion` | **Cobro** |

### Piezas nuevas
- **`Patronato.Caja/Services/GatewaySocketClient.cs`**: abre una conexión TCP nueva por cada llamada, manda una línea JSON (`TransaccionRequest`), lee una línea de vuelta (`TransaccionResponse`). Lanza `GatewayNoDisponibleException` **solo** si no logró ni conectar (no si el Gateway respondió algo, aunque sea un rechazo).
- **`Patronato.Gateway/Services/CoreGatewayService.cs`**: switch con los 15 códigos. Las consultas pasan por `ConsultarAsync` (GET genérico reenviado); los cobros por `EnviarCobroAsync` (ver Fase 4).

### Verificado en vivo
Con un cliente TCP crudo (no el código real, sino un socket independiente imitando exactamente el protocolo) se probaron los 15 códigos contra Core+Gateway reales: todos responden `codigoRespuesta: "00"` con los datos correctos, incluida una venta real que descontó stock y calculó cambio.

---

## 6. Fase 4 — Resiliencia offline (store-and-forward en dos niveles)

Pedido explícito: la Caja debe poder operar sin el Gateway, y el Gateway debe poder operar sin el Core; cuando cada uno vuelve, lo pendiente se sincroniza solo. **Antes de implementar se confirmaron 3 decisiones de diseño con el usuario** (todas se tomaron con la opción recomendada):
1. Un cobro sin conexión **se acepta localmente y se encola** (no se rechaza) — con un recibo PROVISIONAL.
2. Los reintentos son **automáticos en segundo plano** (cada ~15s).
3. Las consultas sin conexión muestran **el último dato bueno en memoria** (no se vacían).

### Nivel Caja → Gateway
- **`Patronato.Caja/Services/OfflineQueueService.cs`**: cola persistida en `%LocalAppData%\PatronatoCaja\cola_pendientes.json` (sobrevive a cerrar la app).
- `ApiService` construye un **recibo provisional** con los datos que la Caja ya tiene en memoria (carrito, sesión de masaje seleccionada, préstamo seleccionado) cuando `GatewayNoDisponibleException` salta en los 4 cobros. El número de recibo lleva el prefijo `OFFLINE-`.
- El catálogo de productos aplica localmente las unidades vendidas sin conexión (`_ajustesStockOffline`) para no "vender de más" mientras esos cobros siguen pendientes de confirmar.
- Caché en memoria para las 7 consultas (`_cacheProductos`, `_cacheMasajes`, etc.) — si no hay conexión, se devuelve lo último bueno.
- `MainWindow` tiene un `DispatcherTimer` (`_syncTimer`, 15s) que llama a `ApiService.SincronizarPendientesAsync()`; un badge "⏳ N pendientes de sincronizar" en la barra de estado inferior se actualiza vía el evento `OfflineQueueService.CambioPendientes`.

### Nivel Gateway → Core
- **`Patronato.Gateway/Services/PendingQueueStore.cs`**: cola persistida en `Patronato.Gateway/cola-pendientes-core.json` (gitignored).
- **`Patronato.Gateway/Services/CoreSyncWorker.cs`**: `BackgroundService` que cada 15s intenta vaciar la cola llamando a `CoreGatewayService.ReenviarPendienteAsync`.
- `CoreGatewayService.EnviarCobroAsync`: si falla la conexión al Core (`HttpRequestException`/`SocketException`/`TaskCanceledException`), en vez de rechazar, encola y responde **código `"01"`** = "aceptada, pendiente de confirmar con el Core" (no es un rechazo, es un nuevo estado intermedio en el esquema de respuestas: `00` aprobada, `01` aceptada-pendiente, `05` rechazada por el Core, `12` código no soportado, `96` error del gateway).
- La Caja trata `"01"` igual que `"00"` para efectos de UI (la operación ya quedó aceptada en algún nivel; solo cambia el mensaje).

### Verificado en vivo (no solo compilado)
1. Con el **Core apagado**, se mandó una venta por el socket crudo → el Gateway respondió `"01"` y la guardó en `cola-pendientes-core.json`.
2. Se **prendió el Core** → sin tocar nada, `CoreSyncWorker` la sincronizó sola dentro de su ciclo; `GET /api/caja/cuadre-caja` ya mostraba la venta reflejada.
3. Con el **Gateway apagado**, se confirmó que conectar al puerto 6000 rebota en ~18ms (`ECONNREFUSED`) — la señal exacta que dispara el guardado local en la Caja.

---

## 7. Fase 5 — `Patronato.Web` (React + Vite + TypeScript + pnpm)

Pedido explícito: proyecto React con pnpm, que consuma el Gateway (nunca el Core), cubriendo los endpoints del Core que la Caja ya no usa (Recepción/Admin).

### Stack
- Vite + React 19 + TypeScript, gestor de paquetes **pnpm**.
- `react-router-dom` (rutas) + `@tanstack/react-query` (fetch/caché/invalidación).
- CSS plano a mano (sin librería de UI), tema institucional (`#1e3a8a`).

### Cambio necesario en el Gateway
El Gateway **no tenía CORS** (solo el Core lo tenía, y es irrelevante porque la Web nunca le habla). Se agregó `AddCors` + `UseCors("AllowAll")` a `Patronato.Gateway/Program.cs` — sin esto, el navegador bloquea las llamadas por política de origen cruzado. Verificado con un preflight `OPTIONS` real: `Access-Control-Allow-Origin: *`.

### Decisión de diseño: REST plano, no el esquema de códigos
La Web llama **directo a las mismas rutas que expone el Core** (p. ej. `GET /api/Beneficiarios`), apuntando al Gateway — que las reenvía transparentemente por su reverse-proxy (`ReverseProxyService` + `MapFallback`, ya existente). **No** se usa el esquema ISO-8583 de códigos para la Web, porque:
- Ese esquema existe para unificar dos canales (socket + REST) en operaciones de **dinero**; la Web nunca cobra dinero.
- Las pantallas de la Web son CRUD/reportes puros (Beneficiarios, Consultas, Rehabilitación, Voluntarios, Sucursales, Usuarios, crear-masaje/crear-prestamo/crear-orden) que el reverse-proxy ya sirve perfecto sin código adicional.

`src/api/client.ts` es el único punto de entrada HTTP, apunta a `VITE_GATEWAY_URL` (por defecto `http://localhost:57171`), y manda `X-Origen: Web` explícito (aunque el Gateway ya lo asume por defecto).

### Reparto de responsabilidades
La Web es **Recepción + Administración**: registra, agenda, otorga, genera cotizaciones, administra sucursales/empleados, y ve reportes. **Nunca cobra dinero** — eso sigue siendo exclusivo de la Caja (los códigos de socket).

### Páginas (13 + landing)

| Ruta | Página | Rol mínimo |
|---|---|---|
| `/` | **Landing pública** (nueva, ver Fase 6) | nadie (pública) |
| `/login` | Login | nadie (pública) |
| `/panel` | Dashboard | cualquiera autenticado |
| `/panel/beneficiarios` | Beneficiarios (lista + alta) | cualquiera |
| `/panel/consultas` | Consultas Oftalmológicas (agendar + diagnóstico) | Admin, Médico Oftalmólogo, Recepcionista |
| `/panel/rehabilitacion` | Rehabilitación (inscribir + graduar) | Admin, Instructor Rehabilitación, Recepcionista |
| `/panel/masajes` | Masajes TACTO (agendar) | Admin, Recepcionista |
| `/panel/prestamos` | Préstamos (otorgar) | Admin, Recepcionista |
| `/panel/ordenes` | Órdenes de Pedido (generar, con selector de inventario) | Admin, Recepcionista |
| `/panel/donaciones` | Donaciones (solo reporte) | Admin, Recepcionista |
| `/panel/voluntarios` | Voluntarios (lista + alta) | Admin, Recepcionista |
| `/panel/transacciones` | Transacciones de Caja (reporte + cuadre) | **Solo Admin** |
| `/panel/sucursales` | Sucursales (lista + alta) | **Solo Admin** |
| `/panel/usuarios` | Empleados y Usuarios (alta + cambiar rol) | **Solo Admin** |

### Autenticación
No hay tokens/JWT en todo el sistema (nunca existieron, ni en Caja). `AuthContext` guarda `{ usuarioId, nombre, rol, sucursalId }` en `sessionStorage` tras llamar a `/api/Usuarios/login` vía el Gateway. El control de acceso es solo de UI (`ProtectedRoute` + filtrado de menú por rol), no hay guardas de seguridad reales a nivel de API — **si se expone a producción, falta añadir autenticación real (tokens) en el Core/Gateway.**

### Verificado en vivo
Con Core+Gateway reales arriba: CORS confirmado con preflight, y se ejecutaron ~20 llamadas reales (login, crear beneficiario/sucursal/usuario/voluntario, cambiar rol, crear consulta + diagnóstico, inscribir + graduar rehabilitación, agendar masaje, otorgar préstamo, crear orden con ítems de inventario reales, listar pendientes, cuadre y transacciones) — todas devuelven exactamente lo que el TypeScript espera. `pnpm build` (type-check + bundle) sin errores.

---

## 8. Fase 6 — Landing page pública

Pedido explícito: la plataforma no debe arrancar directo en el login; debe tener una landing informativa para que cualquiera conozca la institución.

### Cambio de rutas
- `/` ahora es **`LandingPage`** (pública, sin auth).
- Todo el portal interno que antes vivía en `/` se movió a **`/panel`** (`Layout`, `Dashboard`, y las 12 páginas internas — ver tabla arriba).
- `/login` sigue siendo el login; redirige a `/panel` si ya hay sesión, y tiene un link "← Volver al sitio público".
- El menú lateral del portal (`Layout.tsx`) tiene un link "🌐 Ver sitio público" al pie.

### Contenido de la landing (`src/pages/LandingPage.tsx`)
- Nav con marca + botón "Iniciar sesión" (o "Ir al Portal" si ya hay sesión).
- Hero con la frase institucional ("La ceguera no te impide ver con el corazón") y **cifras en vivo** (cantidad de beneficiarios y de sedes, traídas del Gateway con las mismas llamadas `getBeneficiarios`/`getSucursales` que ya existían — sin autenticación, igual que el resto del sistema).
- Grid de los 6 programas (Masajes TACTO, Préstamos, Rehabilitación, Consultas Oftalmológicas, Donaciones, Voluntariado).
- Grid de sedes con dirección/teléfono/encargado **reales**, también en vivo desde el Core.
- Sección de contacto + footer institucional (RNC, dirección, link al portal del personal).

### Verificado en vivo
Las 4 rutas (`/`, `/login`, `/panel`, el módulo `LandingPage.tsx`) responden 200 en el servidor de Vite; los dos endpoints que alimentan las cifras de la landing (`/api/Beneficiarios`, `/api/Sucursales`) devuelven datos reales a través del Gateway. `pnpm build` sin errores.

---

## 9. Archivos clave por proyecto (mapa rápido)

### `Patronato.Core`
- Sin cambios de fondo más allá de la resolución del merge (`ApplicationDbContext.cs`, `Program.cs`). Todos sus controllers/endpoints se mantienen intactos y son la fuente de verdad de rutas/DTOs para Gateway, Caja y Web.

### `Patronato.Gateway`
- `Services/CoreGatewayService.cs` — los 15 códigos + `EnviarCobroAsync` (encola si el Core está caído) + `ReenviarPendienteAsync`.
- `Services/PendingQueueStore.cs` — cola persistida en disco (nuevo).
- `Services/CoreSyncWorker.cs` — `BackgroundService` de reintento (nuevo).
- `Services/ReverseProxyService.cs` — fix de `X-Origen` (antes hardcodeado a "Web").
- `Program.cs` — registro de los nuevos servicios + `AddCors`/`UseCors` (nuevo, para la Web).

### `Patronato.Caja`
- `Services/ApiService.cs` — reescrito por completo: sin `HttpClient`, todo por `GatewaySocketClient`; caché en memoria + cola offline + recibos provisionales.
- `Services/GatewaySocketClient.cs` — cliente TCP (nuevo), con `GatewayNoDisponibleException`.
- `Services/OfflineQueueService.cs` — cola persistida en disco (nuevo).
- `Services/SessionManager.cs` — ahora resuelve el endpoint del socket (host/puerto) a partir de la URL del Gateway.
- `MainWindow.xaml(.cs)` — recortado (sin Gestión de Cajeros, sin Generar Orden, sin lógica de Recepcionista) + temporizador de sincronización + badge de pendientes.
- `Views/LoginWindow.xaml(.cs)` — apunta al Gateway (57171), sin demo de Recepcionista.
- **Borrados**: `Views/AgendarMasajeWindow.*`, `Views/OtorgarPrestamoWindow.*`.
- `Models/CajaModels.cs` — se quitaron los DTOs de las pantallas eliminadas, se agregó `SucursalDto`.

### `Patronato.Web` (proyecto nuevo completo)
- `src/api/{client,endpoints,types}.ts` — capa de API (todo contra el Gateway).
- `src/context/AuthContext.tsx` — sesión en `sessionStorage`.
- `src/components/{Layout,ProtectedRoute,DataTable,PageHeader}.tsx`.
- `src/pages/*.tsx` — 14 páginas (`LandingPage` + `LoginPage` + `Dashboard` + 11 internas).
- `.env.example` (`VITE_GATEWAY_URL`) — `.env` real está gitignored.

---

## 10. Pendientes / cosas a tener en cuenta

- **Nada de esta sesión está comiteado.** Correr `git status` antes de seguir; el usuario pidió explícitamente dejarlo en staging.
- La Web **no probó visualmente en un navegador real** (no hay sesión de escritorio en el entorno de la sesión) — se validó con `pnpm build`, el dev server de Vite sirviendo cada módulo sin error 500, y llamadas reales a cada endpoint. Falta un browser-check manual.
- El WPF de la Caja tampoco se abrió visualmente (mismo motivo) — se validó por build limpio + pruebas de socket crudo reproduciendo exactamente su protocolo.
- No hay autenticación real (tokens) en ningún punto del sistema — el "login" es solo una validación de rol contra la tabla `Usuarios`, sin sesión firmada. Si esto va a producción, es la primera brecha a cerrar.
- El rubro `RubroMicroemprendimiento` tiene 10 valores en el Core; algún combo viejo en la Caja (antes de los recortes) solo mostraba un subconjunto — revisar si quedó algo así en `OtorgarPrestamoWindow` (ya borrada, pero si se recupera algo parecido en la Web, ya `PrestamosPage.tsx` de la Web sí usa los 10 valores completos).
- `cola-pendientes-core.json` (Gateway) y `cola_pendientes.json` (Caja, en `%LocalAppData%\PatronatoCaja\`) son estado runtime — están gitignored, no se versionan.
