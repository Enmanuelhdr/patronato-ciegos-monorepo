# Guion de Exposición — Sistema Integral Patronato Nacional de Ciegos

> Documento pensado para presentarse ante un experto de software (evaluador técnico / comprador). No es solo documentación: es un **guion** con lo que decir, en qué orden, con qué argumentos técnicos respaldar cada afirmación, y cómo responder a las preguntas difíciles que un ingeniero senior va a hacer.

---

## 0. Cómo usar este documento

Cada sección tiene:
- **Qué decir** (el discurso).
- **Por qué importa** (el argumento de venta / la decisión de ingeniería detrás).
- **Evidencia** (archivo y línea concretos para respaldarlo si piden ver código).

Duración estimada de la exposición completa: 25-30 minutos + preguntas.

---

## 1. Apertura — El pitch de 60 segundos

**Qué decir:**

> "Construí un sistema de información integral para el Patronato Nacional de Ciegos: una institución con sucursales físicas, caja registradora, cobro de servicios (masajes terapéuticos, abonos a préstamos de microemprendimiento, donaciones, venta de insumos) y procesos administrativos (consultas oftalmológicas, rehabilitación, voluntariado). El sistema tiene **4 proyectos independientes** que se comunican bajo una regla de oro: **nadie le habla directo a la base de datos de negocio excepto a través de un API Gateway**, exactamente como un cajero automático nunca habla directo al core bancario, sino a través de un switch transaccional. Hay una terminal de punto de venta en WPF que funciona **sin internet** y se sincroniza sola cuando vuelve la conexión, un Gateway que hace lo mismo hacia el core, y un portal web para todo el trabajo administrativo. Todo con logging centralizado, trazabilidad por origen (Caja vs. Web), y una arquitectura que ya separó responsabilidades de negocio por rol antes de que me lo pidieran."

**Por qué importa:** en los primeros 60 segundos hay que dejar claro que esto no es un CRUD de tutorial — es un sistema con **un patrón de integración deliberado** (gateway + dos canales de transporte distintos) y **resiliencia real** (no solo manejo de excepciones, sino colas persistidas en disco con reintento automático). Eso es lo que separa un proyecto de clase de un sistema que un experto tomaría en serio.

---

## 2. Contexto del dominio (por qué el sistema es así)

**Qué decir:**

> "El Patronato no es una empresa de software — es una institución que atiende a personas con discapacidad visual. Eso impone reglas de negocio reales:
> - Los beneficiarios tienen una **condición visual clasificada** (`TipoDiscapacidadVisual`: Baja Visión, Ceguera Total Congénita, Ceguera Total Adquirida) que determina qué servicios reciben.
> - Hay un programa de **microemprendimiento**: la institución presta capital semilla a beneficiarios para que monten negocios propios (colmado, paletera, venta de ropa, masajes itinerantes, crianza de animales, artesanía, etc. — 10 rubros catalogados), y luego cobra las cuotas de ese préstamo en la Caja.
> - Hay un programa de **masajes terapéuticos TACTO**, uno de **rehabilitación** (Braille, bastón guía — con etapas de progreso), uno de **consultas oftalmológicas** de prevención, y uno de **donaciones** institucionales con recibo fiscal opcional (NCF).
> - El personal tiene **5 roles distintos** (Administrador, Cajero, Médico Oftalmólogo, Instructor de Rehabilitación, Recepcionista), cada uno con acceso a funciones distintas del sistema — no es un sistema de un solo tipo de usuario."

**Por qué importa:** esto demuestra que el diseño no fue "hacer tablas genéricas" — hubo un levantamiento real de reglas de negocio de un dominio específico, y el software **refleja esas reglas** en el modelo de datos y en el reparto de pantallas por rol.

**Evidencia:** `Patronato.Core/Enums/*.cs` (`RubroMicroemprendimiento` con 10 valores, `RolUsuario` con 5 valores, `TipoDiscapacidadVisual` con 3 valores, `EtapaRehabilitacion`, `EstadoPrestamo`).

---

## 3. Arquitectura general — el mapa mental

**Qué decir / mostrar (dibujar o proyectar este diagrama):**

```
                    ┌─────────────────────────────┐
                    │     Patronato.Web           │
                    │  React 19 + Vite + TS        │
                    │  (Recepción / Administración)│
                    └──────────────┬───────────────┘
                                   │ HTTP/REST (fetch)
                                   ▼
┌──────────────────┐     ┌─────────────────────────┐
│  Patronato.Caja   │     │   Patronato.Gateway      │
│  WPF (cajero)      │────▶│  ASP.NET Core             │
│  SOLO socket TCP    │TCP │  - Reverse proxy (Web)    │
│  puerto 6000        │    │  - Servidor de sockets    │
│  (sin HTTP alguno)  │    │    (Caja, puerto 6000)    │
└──────────────────┘     │  - Cola de reintento Core │
                           └──────────────┬────────────┘
                                          │ HTTP interno + X-Gateway-Key
                                          ▼
                           ┌─────────────────────────┐
                           │     Patronato.Core        │
                           │  ASP.NET Core + EF Core    │
                           │  + SQLite (patronato.db)   │
                           │  Middleware: rechaza con   │
                           │  401 todo lo que no venga  │
                           │  del Gateway                │
                           └─────────────────────────┘
```

**Qué decir sobre la regla de oro:**

> "El Core tiene un middleware que intercepta **todo** `/api/*` y verifica un header `X-Gateway-Key`. Si no viene, 401 inmediato, sin ejecutar el controlador. Esa clave solo la conoce el Gateway. Ni la Caja ni la Web pueden saltarse esa barrera aunque conocieran la URL del Core — es la misma filosofía de un firewall de aplicación: un único punto de entrada controlado."

**Evidencia:** `Patronato.Core/Program.cs:67-98` (el middleware), `Patronato.Gateway/Program.cs:34-37` (dónde se inyecta la clave en cada llamada saliente del Gateway).

**Tabla de puertos (tenerla a mano para la demo en vivo):**

| Proyecto | HTTP | HTTPS | Otro |
|---|---|---|---|
| `Patronato.Core` | 57141 | 57140 | — |
| `Patronato.Gateway` | 57171 | 57170 | socket TCP **6000** |
| `Patronato.Web` | 5173 (Vite dev) | — | — |

---

## 4. Patronato.Core — el dominio de negocio

**Qué decir:**

> "El Core es la única fuente de verdad. ASP.NET Core 10 + Entity Framework Core 10 contra SQLite. 8 controladores REST: Beneficiarios, Sucursales, Usuarios, Consultas Oftalmológicas, Rehabilitación, Inventario, Voluntarios, y el más grande — Caja, que concentra toda la lógica transaccional de cobro."

**Entidades principales (mostrar la lista, no leerla entera):**
`Beneficiario`, `Usuario`, `Sucursal`, `ConsultaOftalmologica`, `InscripcionRehabilitacion`, `SesionMasajeTacto`, `PrestamoEmprendimiento`, `ProductoArticulo`, `OrdenPedidoCaja`, `TransaccionCaja`, `Donacion`, `Voluntario`, `EmpresaAliada`, `LogEntry`.

**Qué decir sobre CajaController (el corazón transaccional):**

> "`CajaController` no es un CRUD — tiene operaciones de **negocio real**: `CobrarMasaje`, `AbonarPrestamo`, `VenderProducto`, `VenderCarrito`, `RegistrarDonacion`, además de las consultas que alimentan cada pantalla (masajes pendientes, préstamos activos, clientes frecuentes, cuadre de caja, histórico de transacciones). Cada cobro genera un recibo con numeración, descuenta stock cuando aplica, y queda en `TransaccionCaja` para el arqueo."

**Datos semilla (para la demo):**
- 3 sucursales.
- 3 usuarios: `admin/admin123` (Administrador), `cajero/cajero123` (Cajero), `recepcion/recepcion123` (Recepcionista).
- Catálogo inicial de beneficiarios, préstamos y masajes para no arrancar con el sistema vacío.

**Logging del Core:**

> "Cada acción HTTP que pasa el filtro del Gateway queda registrada con log4net directo en una tabla `Logs` de la misma base SQLite — nivel, origen (Caja o Web, según el header que pone el Gateway), logger, mensaje y, si hubo excepción, el stacktrace completo. No es un archivo de texto perdido en el disco: es una tabla consultable con SQL."

**Evidencia:** `Patronato.Core/Program.cs:64-102`, `Patronato.Core/log4net.config`, `Patronato.Core/Entities/LogEntry.cs`.

---

## 5. Patronato.Gateway — la pieza que vende el proyecto

Esta es la sección donde un experto de software se va a interesar de verdad. Dedícale más tiempo.

**Qué decir — el problema que resuelve:**

> "Tenía dos clientes con necesidades de transporte completamente distintas: una terminal de punto de venta (la Caja) que necesita un protocolo ligero, de baja latencia, sin el overhead de HTTP — como un cajero automático — y un portal web que necesita REST normal consumible por `fetch`. En vez de duplicar la lógica de negocio en dos sitios, construí **un único servicio de aplicación** (`CoreGatewayService`) al que ambos canales convergen."

**El esquema de códigos de transacción (inspirado en ISO 8583, el estándar real de transacciones bancarias/ATM):**

> "Cada operación de la Caja viaja como un objeto `{ Mti, Stan, CodigoTransaccion, Datos }` — Message Type Indicator y System Trace Audit Number, igual que una transacción de tarjeta de débito real. Hay 15 códigos: desde `PING` (línea viva, no toca el Core) hasta los 5 que mueven dinero real (`MASAJE_COBRAR`, `PRESTAMO_ABONAR`, `PRODUCTO_VENDER`, `VENTA_CARRITO`, `DONACION_REGISTRAR`). La respuesta usa códigos de respuesta estandarizados: `00` aprobada, `01` aceptada-pendiente (nuevo estado que yo agregué para el store-and-forward), `05` rechazada por el Core, `12` código no soportado, `96` error del gateway."

**Evidencia:** `Patronato.Gateway/Protocol/TransaccionRequest.cs`, `TransaccionResponse.cs`, `Patronato.Gateway/Services/CoreGatewayService.cs:32-56` (el switch de los 15 códigos).

**Los dos puntos de entrada al mismo servicio (clave arquitectónica a resaltar):**

> "`CoreGatewayService.ProcesarAsync` recibe la misma estructura `TransaccionRequest` sin importar si vino por el socket TCP (`CajaSocketServer`, puerto 6000) o por HTTP (`GatewayController`, `POST /api/gateway/transaccion`, pensado para la Web si en algún momento necesita cobrar). **Un solo lugar donde vive la regla de negocio, dos transportes.** Eso es exactamente el patrón que un sistema de pagos real necesita: la lógica no debe depender del canal."

**La decisión que la Web NO use el esquema de códigos:**

> "Aunque el canal existe y funciona, decidí que la Web hable REST plano contra las mismas rutas que expone el Core (`GET /api/Beneficiarios`, etc.), reenviadas transparentemente por un reverse-proxy (`ReverseProxyService` + `MapFallback`). ¿Por qué? Porque el esquema ISO-8583 tiene sentido para unificar canales de **dinero** — la Web nunca cobra dinero, solo hace CRUD y reportes. Forzar un esquema transaccional bancario para listar beneficiarios habría sido sobre-ingeniería. Esa es una decisión consciente, no un olvido — y es la clase de criterio que se espera de alguien que diseña sistemas, no solo que los codifica."

**Evidencia:** `Patronato.Gateway/Services/ReverseProxyService.cs`, `Patronato.Gateway/Program.cs:82-84` (`MapFallback`).

**Resiliencia Gateway → Core (store-and-forward, nivel 1):**

> "Si el Core está caído y llega un cobro, el Gateway **no lo rechaza**. Lo encola en disco (`PendingQueueStore`, un JSON persistido en `cola-pendientes-core.json`) y responde `01` — aceptada, pendiente de confirmar. Un `BackgroundService` (`CoreSyncWorker`) revisa cada 15 segundos si el Core volvió y reenvía la cola en orden FIFO. Si el Core rechaza una pendiente por una razón de negocio (ej. stock agotado), se descarta — reintentarla no cambiaría el resultado. Si sigue sin responder, se queda en cola para el siguiente ciclo."

**Evidencia:** `Patronato.Gateway/Services/CoreGatewayService.cs:119-178` (`EnviarCobroAsync` / `ReenviarPendienteAsync`), `Patronato.Gateway/Services/CoreSyncWorker.cs`, `Patronato.Gateway/Services/PendingQueueStore.cs`.

**CORS para la Web:**

> "El Gateway no tenía CORS porque originalmente solo lo consumía la Caja por socket. Al construir la Web tuve que agregar `AddCors`/`UseCors`, porque un navegador bloquea por política de origen cruzado — verificado con un preflight `OPTIONS` real devolviendo `Access-Control-Allow-Origin: *`."

**Logging del Gateway:**

> "Todo lo que entra por HTTP se loguea (`INFO` en éxito, `ERROR` con excepción completa si falla) con el origen correcto (`Web` o `Caja`, según quién mandó el request). Cada transacción — venga de donde venga — se loguea con su código y su resultado. Y cuando algo se encola por caída del Core, queda un `WARN` explícito con cuántas transacciones hay pendientes en ese momento. Todo a archivo, con rotación diaria y 30 días de retención."

**Evidencia:** `Patronato.Gateway/log4net.config`, `Patronato.Gateway/Program.cs:58-78`, `Patronato.Gateway/Sockets/CajaSocketServer.cs:16,92`.

---

## 6. Patronato.Caja — la terminal de cobro offline-first

**Qué decir:**

> "Esta es la pieza más 'de producción real' del sistema: una aplicación WPF que se comporta como un terminal de punto de venta bancario. Decisión explícita y defendida: **la Caja no tiene ningún `HttpClient`**. Cero HTTP. Toda comunicación —consultas y cobros— viaja por un socket TCP crudo contra el puerto 6000 del Gateway, con el mismo protocolo de 15 códigos descrito arriba."

**Por qué esto es una decisión de diseño, no una limitación:**

> "Un cajero automático no abre una sesión HTTP por cada botón que presionás — mantiene un protocolo de bajo nivel optimizado para eso. Reproduje esa misma filosofía aquí: `GatewaySocketClient` abre una conexión TCP nueva por operación, manda una línea JSON terminada en `\n`, lee una línea de respuesta, cierra. Simple, predecible, y con timeouts explícitos (3s para conectar, 10s para la respuesta) que distinguen 'el Gateway no existe' de 'el Gateway está lento'."

**Módulos funcionales de la Caja (recorrido de pantallas):**
1. **Login** — restringido a roles Cajero y Administrador (no Recepcionista: esa gente no cobra).
2. **Venta POS** — carrito de productos + "Cargar Orden" (cobra una cotización que Recepción generó en la Web).
3. **Masajes TACTO** — cobro de sesiones ya agendadas.
4. **Préstamos** — aplicar abonos a cuotas (nunca otorgar crédito — eso es decisión de comité, se hace en la Web).
5. **Donaciones** — con recibo oficial y opción de comprobante fiscal (NCF).
6. **Arqueo de caja** — KPIs por método de pago y por concepto, con corte del turno.
7. **Historial de recibos** — búsqueda y reimpresión.

**El recorte deliberado (argumento de madurez de producto):**

> "La Caja tenía originalmente pantallas de agendar masajes, otorgar préstamos, generar órdenes y gestionar cajeros — porque el Core las soporta. Las eliminé **todas**, confirmándolo explícitamente antes de tocar código, porque el propio Core ya documentaba en sus comentarios que esas son tareas de Recepción, no de Caja. Una terminal de cobro no debería poder crear deuda ni agendar citas — ese es el tipo de separación de responsabilidades que previene errores operativos reales en un entorno de producción. Esas funciones no se perdieron: reaparecieron en el portal Web, donde sí corresponden."

**Resiliencia Caja → Gateway (store-and-forward, nivel 2 — simétrico al del Gateway):**

> "Si la Caja no logra conectar al socket (y solo en ese caso — nunca si el Gateway respondió un rechazo de negocio), se lanza `GatewayNoDisponibleException`. El cobro **no se pierde**: se construye un recibo **provisional** (prefijo `OFFLINE-...`) con los datos que la Caja ya tiene en memoria, se encola en disco (`OfflineQueueService`, persistido en `%LocalAppData%\PatronatoCaja\cola_pendientes.json` — sobrevive a cerrar la aplicación), y un timer de 15 segundos reintenta sincronizar solo. Mientras tanto, un badge visible '⏳ N pendientes de sincronizar' informa al cajero sin bloquear su trabajo. El catálogo de productos incluso ajusta el stock localmente para no 'vender de más' mientras esas ventas siguen sin confirmar con el Core."

**Evidencia:** `Patronato.Caja/Services/GatewaySocketClient.cs`, `Patronato.Caja/Services/OfflineQueueService.cs`, `Patronato.Caja/Services/ApiService.cs` (caché en memoria + recibos provisionales), `Patronato.Caja/MainWindow.xaml.cs` (`_syncTimer`, badge).

**Logging de la Caja (lo más reciente, recién agregado):**

> "Hasta hace poco la Caja no dejaba ningún rastro persistente de sus errores. Le agregué log4net a nivel de archivo (`%LocalAppData%\PatronatoCaja\logs\caja.log`, rotación diaria, 30 días). Registra login (éxito/rechazo), cada cobro con su resultado, y — lo más valioso operacionalmente — **las transiciones de conectividad**: un `WARN` el instante en que se pierde la conexión con el Gateway, un `INFO` cuando se recupera, sin inundar el log con una línea por cada clic mientras la caída dura. También queda registrado cada recibo que se encola offline (con su número provisional) y cada uno que se sincroniza después."

**Evidencia:** `Patronato.Caja/log4net.config`, `Patronato.Caja/App.xaml.cs` (`OnStartup`), `Patronato.Caja/Services/ApiService.cs` (propiedad `SinConexionGateway` con logging en la transición).

---

## 7. Patronato.Web — el portal de Recepción y Administración

**Qué decir:**

> "React 19 + TypeScript + Vite, gestionado con pnpm. `@tanstack/react-query` para fetch/caché/invalidación, `react-router-dom` para rutas, CSS institucional hecho a mano — sin librería de componentes de terceros, con la paleta institucional del Patronato (`#1e3a8a`)."

**Reparto de responsabilidades (resaltar la simetría con la Caja):**

> "Si la Caja es 'cobrar dinero', la Web es 'todo lo demás': registrar beneficiarios, agendar consultas oftalmológicas y masajes, inscribir y graduar beneficiarios de rehabilitación, otorgar préstamos (la decisión de crédito, no el cobro), generar cotizaciones de órdenes de pedido, administrar sucursales y empleados, y ver reportes de caja. **Nunca cobra dinero** — eso sigue siendo exclusivo de la terminal física."

**14 páginas, control de acceso por rol:**

| Página | Rol mínimo |
|---|---|
| Landing pública | nadie |
| Login | nadie |
| Dashboard | cualquiera autenticado |
| Beneficiarios | cualquiera |
| Consultas Oftalmológicas | Admin, Médico Oftalmólogo, Recepcionista |
| Rehabilitación | Admin, Instructor Rehabilitación, Recepcionista |
| Masajes TACTO | Admin, Recepcionista |
| Préstamos | Admin, Recepcionista |
| Órdenes de Pedido | Admin, Recepcionista |
| Donaciones | Admin, Recepcionista |
| Voluntarios | Admin, Recepcionista |
| Transacciones de Caja | **Solo Admin** |
| Sucursales | **Solo Admin** |
| Usuarios/Empleados | **Solo Admin** |

**La landing pública (argumento de producto, no solo de ingeniería):**

> "No tiene sentido que una institución pública arranque directo en un login — eso es software interno disfrazado de producto. Agregué una landing (`/`) con hero institucional, **cifras en vivo** (cantidad real de beneficiarios y sedes, traídas del Gateway sin autenticación), los 6 programas que ofrece la institución, y el directorio de sedes con dirección/teléfono reales. El portal interno se movió a `/panel`. Esto es pensar en el usuario final — no todo el mundo que visita el sitio es empleado."

**Lo que hay que admitir sin rodeos (y cómo lo giro a favor):**

> "No hay JWT ni tokens firmados — `AuthContext` guarda la sesión en `sessionStorage` tras validar contra `/api/Usuarios/login`, y el control de acceso es de **UI** (rutas protegidas + menú filtrado por rol), no hay una guarda de seguridad real a nivel de API. Lo digo de entrada porque un evaluador técnico lo va a encontrar en dos minutos si no lo digo yo primero. La razón: el alcance de este proyecto fue demostrar la arquitectura de integración y la resiliencia offline — no un sistema de identidad. Es la primera línea del roadmap si esto pasa a producción, y la arquitectura ya está lista para eso: basta con que el middleware del Core (el mismo que ya valida `X-Gateway-Key`) también valide un JWT por usuario, sin tocar Caja ni Web más que el header que ya mandan."

**Evidencia:** `Patronato.Web/src/context/AuthContext.tsx`, `Patronato.Web/src/components/ProtectedRoute.tsx`.

---

## 8. Demo en vivo — guion paso a paso

Si el experto quiere ver el sistema correr, este es el orden recomendado (ya fue verificado en sesiones anteriores, no es especulativo):

1. **Arrancar Core** → mostrar Swagger (`/swagger`) con los 8 controladores.
2. **Arrancar Gateway** → mostrar su propio Swagger, y el servidor de sockets escuchando en 6000 (log de arranque).
3. **Abrir la Web** (`pnpm dev`) → mostrar la landing pública con cifras en vivo, luego login como `admin/admin123`, crear un beneficiario, mostrar que aparece reflejado.
4. **Abrir la Caja** → login como `cajero/cajero123`, cobrar una venta POS, mostrar el recibo con numeración real.
5. **Apagar el Core** → desde la Caja, cobrar otra venta: mostrar que se acepta igual, con recibo `OFFLINE-...` y el badge de pendientes.
6. **Prender el Core de nuevo** → sin tocar nada, esperar ~15s y mostrar que el badge baja a cero solo — la venta ya aparece en el arqueo.
7. **Apagar el Gateway** con el Core arriba → mostrar que la Caja detecta la caída casi instantáneo (~18ms de `ECONNREFUSED`) y sigue el mismo flujo de cola.
8. **Mostrar los logs**: la tabla `Logs` del Core (vía una consulta SQL rápida), `Patronato.Gateway/logs/gateway.log`, y `%LocalAppData%\PatronatoCaja\logs\caja.log` — las tres fuentes narrando la misma caída desde tres ángulos distintos.

**Por qué este guion de demo vende el proyecto:** no muestra pantallas bonitas — muestra que el sistema **no se cae** cuando una pieza falla, y que cada pieza deja evidencia de lo que pasó. Eso es lo que un experto evalúa: comportamiento bajo falla, no solo el camino feliz.

---

## 9. Observabilidad — resumen para cerrar el argumento técnico

| Componente | Mecanismo | Destino | Qué registra |
|---|---|---|---|
| Core | log4net `AdoNetAppender` | Tabla `Logs` en `patronato.db` | Toda acción HTTP post-autenticación + excepciones, con origen (Caja/Web) |
| Gateway | log4net `RollingFileAppender` | `logs/gateway.log` | Toda transacción (consulta o cobro) con su código y resultado, encolados y reintentos |
| Caja | log4net `RollingFileAppender` (agregado en esta fase) | `%LocalAppData%\PatronatoCaja\logs\caja.log` | Login, cobros, transiciones de conectividad, encolado/sincronización offline |
| Web | *(sin logging — gap conocido)* | — | — |

**Qué decir sobre el gap de la Web:**

> "La Web es la única pieza sin logging persistente — es una decisión de alcance, no un olvido: es una SPA sin backend propio, así que cualquier logging ahí implicaría o mandar eventos al Gateway (viable, pendiente) o un servicio de telemetría de terceros (fuera del alcance de este proyecto)."

---

## 10. Por qué esto vale — argumentos de venta directos

Usar estos puntos si el experto pregunta "¿y por qué es mejor que [alternativa obvia]?":

1. **No es un monolito con módulos — son 4 ejecutables independientes** que se pueden desplegar, escalar y fallar por separado. El Gateway puede estar en una VM distinta del Core sin cambiar una línea de código de la Caja.
2. **Dos protocolos de transporte conviviendo sobre la misma lógica de negocio** (socket ISO-8583-like + REST), sin duplicar reglas — la clase de decisión que evita el "tengo que arreglar esto en dos lugares" seis meses después.
3. **Resiliencia offline de verdad, en dos niveles independientes**, no un `try/catch` que muestra "error de red" — colas persistidas en disco, reintento automático, y una semántica de respuesta (`01`) que distingue "rechazado" de "aceptado pero pendiente".
4. **El recorte de alcance de la Caja fue una decisión de producto**, no una limitación técnica — demuestra criterio de diseño, no solo capacidad de escribir código.
5. **Logging centralizado y correlacionable por origen** en 3 de los 4 componentes, con una estrategia clara para el cuarto.
6. **Honestidad sobre lo que falta** (autenticación real) con un plan concreto de cómo cerrarlo sin rediseñar nada — eso es lo que distingue a alguien que entiende el sistema completo de alguien que solo lo construyó por partes.

---

## 11. Preguntas difíciles que un experto va a hacer (preparar la respuesta antes)

**"¿Por qué socket crudo y no gRPC o SignalR?"**
> "Evalué el costo de aprendizaje/dependencias contra el beneficio real: la Caja hace una operación a la vez, de forma síncrona, con un volumen bajo (un cajero, no miles de conexiones concurrentes). Un socket TCP de línea-JSON es exactamente lo que un terminal de punto de venta necesita, sin el overhead de un framework RPC completo. Si este sistema necesitara broadcast en tiempo real a múltiples cajas simultáneas, ahí sí evaluaría SignalR."

**"¿Qué pasa si dos cajeros cobran al mismo tiempo y hay un conflicto de stock?"**
> "Hoy el descuento de stock ocurre en el Core al momento de la venta confirmada — es el único punto de verdad. El ajuste de stock offline en la Caja es solo una proyección local para no mostrarle al cajero un número incorrecto mientras esa venta sigue en cola; nunca es la fuente de verdad. Es una limitación conocida bajo alta concurrencia con el Core caído simultáneamente en dos cajas — está documentada, no escondida."

**"¿Por qué SQLite y no un motor de producción (SQL Server/Postgres)?"**
> "Para el alcance de este proyecto (una institución con pocas sucursales, no miles de transacciones por segundo), SQLite con WAL es suficiente y simplifica el despliegue — una sola institución sin DBA dedicado. El acceso está detrás de EF Core, así que migrar el proveedor es un cambio de connection string y provider, no un rediseño del dominio."

**"¿Cómo se prueba todo esto? ¿Hay tests automatizados?"**
> (Responder con honestidad si no los hay, y señalar qué sí se verificó: pruebas funcionales en vivo documentadas — los 15 códigos de transacción probados contra Core+Gateway reales, los tres escenarios de caída reproducidos y confirmados, `pnpm build` limpio en la Web. Si no hay suite de tests automatizados, decirlo y proponerlo como siguiente paso, no inventar que existen.)

**"¿Qué pasa si el Gateway se cae mientras tiene transacciones en su propia cola sin sincronizar?"**
> "La cola de `PendingQueueStore` está persistida en disco (`cola-pendientes-core.json`), no en memoria — un reinicio del Gateway no pierde las transacciones pendientes. Al volver a levantar, el `CoreSyncWorker` las retoma donde quedaron."

---

## 12. Roadmap — qué sigue si esto se vende/continúa

1. Autenticación real (JWT) en el middleware del Core — ya hay el gancho (`X-Gateway-Key`) donde insertarlo.
2. Logging/telemetría para la Web (mínimo: enviar errores de cliente al Gateway para persistirlos).
3. Suite de pruebas automatizadas (unit + integración) sobre `CoreGatewayService` y el protocolo de socket.
4. Motor de base de datos de producción si la institución crece a múltiples sedes con alto volumen concurrente.
5. Revisar concurrencia de stock bajo caída simultánea de Core en múltiples cajas.

---

## 13. Cierre

**Qué decir:**

> "Este proyecto no es una demo de CRUD con una capa bonita encima. Es un ejercicio deliberado de **arquitectura de integración**: dos transportes, un dominio de negocio, resiliencia en dos niveles independientes, separación de responsabilidades por rol validada contra las reglas reales del negocio, y trazabilidad de extremo a extremo. Lo que falta — autenticación real, tests, telemetría de la Web — está identificado, acotado, y no requiere repensar nada de lo ya construido. Eso es, en mi opinión, la diferencia entre un proyecto de portafolio y un sistema que alguien podría empezar a operar."
