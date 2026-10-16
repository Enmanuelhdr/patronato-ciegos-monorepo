using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Patronato.Core.Data;
using Patronato.Core.Entities;
using Patronato.Core.Servicios;

namespace Patronato.Core.Monitoreo
{
    // Panel de monitoreo del Core: acceso DIRECTO (fuera de /api, por eso no pasa por
    // el middleware de X-Gateway-Key), pensado solo para soporte/mantenimiento interno.
    // El acceso ya no es una clave compartida: se entra con usuario y contraseña reales
    // (la misma tabla Usuario/CoreRole de siempre) y el rol determina qué se puede hacer
    // — ver MonitorAuth y PermisosCore.MonitorVer/MonitorGestionar.
    public static class MonitorEndpoints
    {
        private record LoginRequest(string NombreUsuario, string Password);

        public static void MapMonitorEndpoints(this WebApplication app)
        {
            app.MapGet("/monitor", () => Results.Content(PaginaHtml, "text/html"));

            // Compartido con /monitor/operaciones (mismo token, mismo sessionStorage en el
            // navegador): no pasa por el Gateway a propósito, igual que el resto del panel.
            app.MapPost("/monitor/login", async (ApplicationDbContext db, IPasswordHasher<Usuario> hasher, JwtService jwt, LoginRequest body) =>
            {
                var resultado = await AutenticacionCoreService.IntentarLogin(db, hasher, body.NombreUsuario, body.Password);
                if (!resultado.Exito)
                    return Results.Json(new { mensaje = resultado.Error }, statusCode: resultado.StatusCodeError);

                if (!resultado.Permisos.Contains(PermisosCore.MonitorVer))
                    return Results.Json(new { mensaje = "Tu usuario no tiene acceso al panel de soporte." }, statusCode: 403);

                return Results.Json(new
                {
                    nombre = resultado.Usuario!.NombreCompleto,
                    rol = resultado.RolNombre,
                    permisos = resultado.Permisos,
                    token = jwt.GenerarToken(resultado.Usuario, resultado.RolNombre, resultado.Permisos),
                });
            });

            app.MapGet("/monitor/datos", async (HttpContext ctx, ApplicationDbContext db, string? nivel, string? origen, int? limite) =>
            {
                if (!MonitorAuth.TienePermiso(ctx, PermisosCore.MonitorVer))
                    return Results.Json(new { mensaje = "No autorizado." }, statusCode: 401);

                var haceUnaHora = DateTime.Now.AddHours(-1);

                var provider = db.Database.ProviderName ?? "SQLite";
                var motorDb = provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) ? "SQLite" :
                              provider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) ? "SQL Server" :
                              provider.Split('.').LastOrDefault() ?? "SQL";

                var sw = System.Diagnostics.Stopwatch.StartNew();
                var baseDatosOk = true;
                try { await db.Database.ExecuteSqlRawAsync("SELECT 1"); }
                catch { baseDatosOk = false; }
                sw.Stop();
                var latenciaDbMs = sw.ElapsedMilliseconds;

                var proceso = System.Diagnostics.Process.GetCurrentProcess();
                var memoriaMb = Math.Round(GC.GetTotalMemory(false) / (1024.0 * 1024.0), 2);
                var tiempoActivo = DateTime.Now - proceso.StartTime;

                var consultaLogs = db.Logs.AsQueryable();
                if (!string.IsNullOrWhiteSpace(nivel)) consultaLogs = consultaLogs.Where(l => l.Nivel == nivel);
                if (!string.IsNullOrWhiteSpace(origen)) consultaLogs = consultaLogs.Where(l => l.Origen == origen);

                var logs = await consultaLogs
                    .OrderByDescending(l => l.Fecha)
                    .Take(Math.Clamp(limite ?? 150, 1, 500))
                    .Select(l => new { l.Id, l.Fecha, l.Nivel, l.Origen, l.Logger, l.Mensaje, l.Excepcion })
                    .ToListAsync();

                var ultimaHora = await db.Logs.Where(l => l.Fecha >= haceUnaHora).ToListAsync();
                var totalUltimaHora = ultimaHora.Count;
                var erroresUltimaHora = ultimaHora.Count(l => l.Nivel == "ERROR");
                var warningsUltimaHora = ultimaHora.Count(l => l.Nivel == "WARN");
                var infoUltimaHora = ultimaHora.Count(l => l.Nivel == "INFO");

                var resultado = new
                {
                    horaServidor = DateTime.Now,
                    saludSistema = new
                    {
                        baseDatosOk,
                        latenciaDbMs,
                        motorDb,
                        memoriaMb,
                        tiempoActivo = $"{(int)tiempoActivo.TotalHours}h {tiempoActivo.Minutes}m",
                        hilos = proceso.Threads.Count,
                    },
                    conteos = new
                    {
                        beneficiarios = await db.Beneficiarios.CountAsync(),
                        sucursales = await db.Sucursales.CountAsync(),
                        usuarios = await db.Usuarios.CountAsync(),
                        prestamos = await db.PrestamosEmprendimiento.CountAsync(),
                        transaccionesCaja = await db.TransaccionesCaja.CountAsync(),
                        donaciones = await db.Donaciones.CountAsync(),
                        voluntarios = await db.Voluntarios.CountAsync(),
                        masajes = await db.SesionesMasaje.CountAsync(),
                        consultas = await db.ConsultasOftalmologicas.CountAsync(),
                        rehabilitacion = await db.InscripcionesRehabilitacion.CountAsync(),
                        ordenes = await db.OrdenesPedidoCaja.CountAsync(),
                    },
                    ultimaHora = new
                    {
                        errores = erroresUltimaHora,
                        warnings = warningsUltimaHora,
                        info = infoUltimaHora,
                        total = totalUltimaHora,
                        porOrigen = ultimaHora.GroupBy(l => string.IsNullOrWhiteSpace(l.Origen) ? "Desconocido" : l.Origen)
                                             .ToDictionary(g => g.Key, g => g.Count()),
                    },
                    logs,
                };

                return Results.Json(resultado);
            });
        }

        private const string PaginaHtml = """
<!doctype html>
<html lang="es">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1.0" />
<title>Monitor del Sistema — Patronato.Core</title>
<style>
  :root { color-scheme: dark; }
  * { box-sizing: border-box; }
  body { margin: 0; font-family: "Segoe UI", system-ui, sans-serif; background: #0b1120; color: #e2e8f0; }
  header { display: flex; align-items: center; justify-content: space-between; padding: 14px 22px; background: #111827; border-bottom: 1px solid #1f2937; }
  header h1 { margin: 0; font-size: 16px; }
  header small { color: #94a3b8; }
  .estado { display: flex; align-items: center; gap: 8px; font-size: 13px; }
  .punto { width: 10px; height: 10px; border-radius: 50%; background: #64748b; }
  .punto.ok { background: #22c55e; box-shadow: 0 0 8px #22c55e; }
  .punto.mal { background: #ef4444; box-shadow: 0 0 8px #ef4444; }
  main { padding: 20px; max-width: 1300px; margin: 0 auto; }
  .login { max-width: 380px; margin: 80px auto; background: #111827; border: 1px solid #1f2937; border-radius: 10px; padding: 24px; text-align: center; }
  .login input { width: 100%; padding: 10px; margin: 8px 0; border-radius: 6px; border: 1px solid #334155; background: #0b1120; color: #e2e8f0; }
  .login button:not(.btn-eye), .toolbar button, .btn { background: #2563eb; color: white; border: none; padding: 8px 14px; border-radius: 6px; cursor: pointer; font-size: 12px; font-weight: 600; }
  .login > button:not(.btn-eye) { width: 100%; }
  .btn-eye { position: absolute !important; right: 10px !important; top: 50% !important; transform: translateY(-50%) !important; width: 30px !important; height: 30px !important; min-width: 0 !important; background: transparent !important; border: none !important; cursor: pointer !important; font-size: 18px !important; padding: 0 !important; margin: 0 !important; color: #94a3b8 !important; display: inline-flex !important; align-items: center !important; justify-content: center !important; z-index: 10 !important; }
  .btn-eye:hover { background: transparent !important; color: #38bdf8 !important; }
  .login button:hover, .toolbar button:hover, .btn:hover { background: #1d4ed8; }
  .oculto { display: none !important; }
  .salud-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 12px; margin-bottom: 18px; }
  .tarjeta-salud { background: #111827; border: 1px solid #1f2937; border-radius: 10px; padding: 14px; }
  .tarjeta-salud span.etiqueta { display: block; font-size: 11px; color: #94a3b8; text-transform: uppercase; letter-spacing: .04em; }
  .tarjeta-salud span.valor { display: block; font-size: 24px; font-weight: 700; margin-top: 4px; }
  .tarjeta-salud.verde span.valor { color: #4ade80; }
  .tarjeta-salud.rojo span.valor { color: #f87171; }
  .tarjeta-salud.ambar span.valor { color: #fbbf24; }
  .tarjeta-salud.azul span.valor { color: #38bdf8; }
  .graficos-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(320px, 1fr)); gap: 14px; margin-bottom: 18px; }
  .panel-grafico { background: #111827; border: 1px solid #1f2937; border-radius: 10px; padding: 16px; }
  .panel-grafico h3 { margin: 0 0 12px; font-size: 13px; color: #e2e8f0; }
  .barra-track { height: 14px; background: #1e293b; border-radius: 7px; overflow: hidden; display: flex; }
  .barra-seg { height: 100%; transition: width 0.3s ease; }
  .barra-seg.err { background: #ef4444; }
  .barra-seg.warn { background: #f59e0b; }
  .barra-seg.inf { background: #3b82f6; }
  .leyenda { display: flex; gap: 14px; margin-top: 10px; font-size: 11px; color: #94a3b8; flex-wrap: wrap; }
  .leyenda span { display: flex; align-items: center; gap: 5px; }
  .dot { width: 8px; height: 8px; border-radius: 50%; }
  .panel { background: #111827; border: 1px solid #1f2937; border-radius: 10px; padding: 16px; margin-bottom: 20px; }
  .panel h2 { margin: 0 0 12px; font-size: 14px; }
  .toolbar { display: flex; gap: 8px; margin-bottom: 12px; flex-wrap: wrap; align-items: center; }
  .toolbar input, .toolbar select { background: #0b1120; color: #e2e8f0; border: 1px solid #334155; border-radius: 6px; padding: 7px 10px; font-size: 12px; }
  .toolbar .auto { font-size: 12px; color: #94a3b8; margin-left: auto; }
  table { width: 100%; border-collapse: collapse; font-size: 12px; }
  th { text-align: left; padding: 8px; border-bottom: 1px solid #1f2937; color: #94a3b8; font-weight: 600; }
  td { padding: 8px; border-bottom: 1px solid #1a2332; vertical-align: top; }
  .nivel { padding: 2px 8px; border-radius: 10px; font-size: 11px; font-weight: 700; }
  .nivel-ERROR { background: #450a0a; color: #f87171; }
  .nivel-WARN { background: #451a03; color: #fbbf24; }
  .nivel-INFO { background: #0c2d48; color: #60a5fa; }
  .mono { font-family: ui-monospace, Consolas, monospace; white-space: pre-wrap; word-break: break-all; }
</style>
</head>
<body>

<div id="login" class="login">
  <h2>🔒 Auditoría y Monitoreo Institucional</h2>
  <p style="color:#94a3b8;font-size:13px;">Acceso administrativo y monitoreo de salud del sistema. Inicia sesión con tus credenciales institucionales.</p>
  <input id="usuario" placeholder="Usuario" autocomplete="username" />
  <div style="position:relative;width:100%;margin-bottom:12px;">
    <input id="clave" type="password" placeholder="Contraseña" autocomplete="current-password" style="width:100%;box-sizing:border-box;padding-right:42px;margin-bottom:0;" />
    <button type="button" class="btn-eye" onclick="const c=document.getElementById('clave');c.type=c.type==='password'?'text':'password';this.textContent=c.type==='password'?'👁️':'🙈';" title="Ver u ocultar contraseña">👁️</button>
  </div>
  <button onclick="conectar()">Iniciar Sesión</button>
  <p id="loginError" style="color:#f87171;font-size:12px;"></p>
</div>

<div id="app" class="oculto">
  <header>
    <div>
      <h1>Patronato Nacional de Ciegos · Centro de Monitoreo y Salud</h1>
      <small>Consola de observabilidad, eventos en vivo y diagnóstico del servidor</small>
    </div>
    <div class="estado">
      <a href="/monitor/operaciones" style="color:#60a5fa;text-decoration:none;font-size:13px;margin-right:16px;">⚙ Gestión Operativa →</a>
      <span id="sesionTexto" style="color:#94a3b8;font-size:12px;margin-right:12px;"></span>
      <button onclick="cerrarSesion()" style="background:#374151;font-size:12px;padding:6px 10px;">Salir</button>
      <span id="puntoEstado" class="punto"></span>
      <span id="textoEstado">Conectando...</span>
    </div>
  </header>

  <main>
    <!-- Tarjetas de Salud del Sistema y Conectividad -->
    <div class="salud-grid">
      <div class="tarjeta-salud verde" id="cardDb">
        <span class="etiqueta" id="lblDbMotor">Base de Datos</span>
        <span class="valor" id="vDbEstado">Conectada</span>
        <small id="vDbLatencia" style="color:#94a3b8;font-size:11px;">Latencia: — ms</small>
      </div>
      <div class="tarjeta-salud azul">
        <span class="etiqueta">Consumo de Memoria RAM</span>
        <span class="valor" id="vMemoria">— MB</span>
        <small id="vHilos" style="color:#94a3b8;font-size:11px;">Hilos en ejecución: —</small>
      </div>
      <div class="tarjeta-salud">
        <span class="etiqueta">Tiempo Activo del Proceso</span>
        <span class="valor" id="vUptime" style="font-size:20px;">—</span>
        <small id="vHoraServidor" style="color:#94a3b8;font-size:11px;">Hora servidor: —</small>
      </div>
      <div class="tarjeta-salud rojo">
        <span class="etiqueta">Errores (Última hora)</span>
        <span class="valor" id="vErrores">0</span>
        <small id="vWarnings" style="color:#fbbf24;font-size:11px;">Warnings: 0</small>
      </div>
    </div>

    <!-- Gráficos de Distribución de Logs (Última Hora) -->
    <div class="graficos-grid">
      <div class="panel-grafico">
        <h3>Distribución de Severidad de Logs (Última hora)</h3>
        <div class="barra-track" id="barraSeveridad">
          <div class="barra-seg err" id="segError" style="width:0%"></div>
          <div class="barra-seg warn" id="segWarn" style="width:0%"></div>
          <div class="barra-seg inf" id="segInfo" style="width:100%"></div>
        </div>
        <div class="leyenda">
          <span><span class="dot" style="background:#ef4444;"></span> Errores: <strong id="lblErr">0</strong></span>
          <span><span class="dot" style="background:#f59e0b;"></span> Advertencias: <strong id="lblWarn">0</strong></span>
          <span><span class="dot" style="background:#3b82f6;"></span> Informativos: <strong id="lblInfo">0</strong></span>
          <span>Total: <strong id="lblTotal">0</strong></span>
        </div>
      </div>

      <div class="panel-grafico">
        <h3>Actividad por Origen de Solicitud (Última hora)</h3>
        <div class="barra-track" id="barraOrigen">
          <div class="barra-seg" id="segCaja" style="background:#10b981;width:0%"></div>
          <div class="barra-seg" id="segWeb" style="background:#0284c7;width:0%"></div>
          <div class="barra-seg" id="segCore" style="background:#8b5cf6;width:0%"></div>
          <div class="barra-seg" id="segOtro" style="background:#64748b;width:100%"></div>
        </div>
        <div class="leyenda">
          <span><span class="dot" style="background:#10b981;"></span> Caja: <strong id="lblCaja">0</strong></span>
          <span><span class="dot" style="background:#0284c7;"></span> Web: <strong id="lblWeb">0</strong></span>
          <span><span class="dot" style="background:#8b5cf6;"></span> Core / Panel: <strong id="lblCore">0</strong></span>
          <span><span class="dot" style="background:#64748b;"></span> Otros: <strong id="lblOtro">0</strong></span>
        </div>
      </div>
    </div>

    <!-- Tabla y Visor de Logs con Búsqueda Sanitizada -->
    <div class="panel">
      <h2>Bitácora de Eventos y Auditoría (Tabla Logs)</h2>
      <div class="toolbar">
        <input id="buscarTexto" placeholder="Buscar en mensaje, excepción o logger..." oninput="filtrarLocal()" style="min-width:240px;" />
        <select id="filtroNivel" onchange="cargar()">
          <option value="">Todos los niveles</option>
          <option value="ERROR">ERROR</option>
          <option value="WARN">WARN</option>
          <option value="INFO">INFO</option>
        </select>
        <select id="filtroOrigen" onchange="cargar()">
          <option value="">Todos los orígenes</option>
          <option value="Caja">Caja</option>
          <option value="Web">Web</option>
          <option value="PanelSoporte">PanelSoporte</option>
          <option value="Desconocido">Desconocido</option>
        </select>
        <button onclick="cargar()">Refrescar</button>
        <button onclick="exportarLogsCSV()" style="background:#059669;">📥 Exportar CSV</button>
        <label class="auto"><input type="checkbox" id="autoRefresh" checked onchange="toggleAuto()" /> Auto-refrescar cada 5s</label>
      </div>

      <table>
        <thead>
          <tr>
            <th style="width:140px;">Fecha y Hora</th>
            <th style="width:80px;">Nivel</th>
            <th style="width:90px;">Origen</th>
            <th style="width:180px;">Logger</th>
            <th>Mensaje y Detalle Técnico</th>
          </tr>
        </thead>
        <tbody id="tablaLogs"></tbody>
      </table>
    </div>
  </main>
</div>

<script>
let token = sessionStorage.getItem('monitorToken') || '';
let timer = null;
let todosLosLogs = [];

if (token) conectar(true);

async function conectar(silencioso) {
  if (!silencioso) {
    const usuario = document.getElementById('usuario').value.trim();
    const clave = document.getElementById('clave').value;
    if (!usuario || !clave) return;
    const r = await fetch('/monitor/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ nombreUsuario: usuario, password: clave })
    });
    const datos = await r.json().catch(() => ({}));
    if (!r.ok) {
      document.getElementById('loginError').textContent = datos.mensaje || 'Credenciales inválidas.';
      return;
    }
    token = datos.token;
    sessionStorage.setItem('monitorToken', token);
    sessionStorage.setItem('monitorSesion', JSON.stringify({ nombre: datos.nombre, rol: datos.rol }));
  }

  fetch('/monitor/datos', { headers: { 'Authorization': 'Bearer ' + token } }).then(async r => {
    if (r.status === 401 || r.status === 403) {
      sessionStorage.removeItem('monitorToken');
      document.getElementById('loginError').textContent = 'Sesión expirada o sin permisos de soporte.';
      return;
    }
    const sesion = JSON.parse(sessionStorage.getItem('monitorSesion') || '{}');
    document.getElementById('sesionTexto').textContent = sesion.nombre ? `${sesion.nombre} (${sesion.rol})` : '';
    document.getElementById('login').classList.add('oculto');
    document.getElementById('app').classList.remove('oculto');
    pintar(await r.json());
    toggleAuto();
  }).catch(() => {
    document.getElementById('loginError').textContent = 'No se pudo conectar al Core.';
  });
}

function cerrarSesion() {
  clearInterval(timer);
  sessionStorage.removeItem('monitorToken');
  sessionStorage.removeItem('monitorSesion');
  location.reload();
}

function cargar() {
  const nivel = document.getElementById('filtroNivel').value;
  const origen = document.getElementById('filtroOrigen').value;
  const qs = new URLSearchParams({ nivel, origen, limite: 200 });
  fetch('/monitor/datos?' + qs, { headers: { 'Authorization': 'Bearer ' + token } }).then(async r => {
    if (r.status === 401 || r.status === 403) { clearInterval(timer); cerrarSesion(); return; }
    pintar(await r.json());
  });
}

function toggleAuto() {
  clearInterval(timer);
  if (document.getElementById('autoRefresh').checked) {
    timer = setInterval(() => {
      if (!document.hidden) cargar();
    }, 5000);
  }
}

document.addEventListener('visibilitychange', () => {
  if (!document.hidden && document.getElementById('autoRefresh')?.checked) {
    cargar();
  }
});

// Sanitización rigurosa para prevenir XSS (Hallazgo Crítico #3)
function esc(s) {
  if (s === null || s === undefined) return '';
  return String(s)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

function pintar(d) {
  const salud = d.saludSistema || {};
  const dbOk = salud.baseDatosOk !== false;

  document.getElementById('puntoEstado').className = 'punto ' + (dbOk ? 'ok' : 'mal');
  document.getElementById('textoEstado').textContent = dbOk
    ? `Base de Datos OK · Latencia: ${salud.latenciaDbMs ?? 0} ms`
    : 'Base de Datos INACCESIBLE';

  const cardDb = document.getElementById('cardDb');
  cardDb.className = 'tarjeta-salud ' + (dbOk ? 'verde' : 'rojo');
  document.getElementById('lblDbMotor').textContent = `Base de Datos (${salud.motorDb || 'SQLite'})`;
  document.getElementById('vDbEstado').textContent = dbOk ? 'Operativa' : 'Fallo Conexión';
  document.getElementById('vDbLatencia').textContent = `Latencia query: ${salud.latenciaDbMs ?? 0} ms`;

  document.getElementById('vMemoria').textContent = (salud.memoriaMb ?? 0) + ' MB';
  document.getElementById('vHilos').textContent = `Hilos activos: ${salud.hilos ?? '—'}`;
  document.getElementById('vUptime').textContent = salud.tiempoActivo ?? '—';
  document.getElementById('vHoraServidor').textContent = 'Hora: ' + new Date(d.horaServidor).toLocaleTimeString();

  const uh = d.ultimaHora || {};
  document.getElementById('vErrores').textContent = uh.errores || 0;
  document.getElementById('vWarnings').textContent = `Warnings: ${uh.warnings || 0}`;

  // Gráfico 1: Severidad
  const totalLogs1h = Math.max(uh.total || 0, 1);
  const pErr = ((uh.errores || 0) / totalLogs1h) * 100;
  const pWarn = ((uh.warnings || 0) / totalLogs1h) * 100;
  const pInf = ((uh.info || 0) / totalLogs1h) * 100;

  document.getElementById('segError').style.width = pErr + '%';
  document.getElementById('segWarn').style.width = pWarn + '%';
  document.getElementById('segInfo').style.width = (uh.total ? pInf : 100) + '%';

  document.getElementById('lblErr').textContent = uh.errores || 0;
  document.getElementById('lblWarn').textContent = uh.warnings || 0;
  document.getElementById('lblInfo').textContent = uh.info || 0;
  document.getElementById('lblTotal').textContent = uh.total || 0;

  // Gráfico 2: Origen
  const po = uh.porOrigen || {};
  const totalOrig = Math.max(Object.values(po).reduce((a, b) => a + b, 0), 1);
  const pCaja = ((po.Caja || 0) / totalOrig) * 100;
  const pWeb = ((po.Web || 0) / totalOrig) * 100;
  const pCore = (((po.PanelSoporte || 0) + (po.Core || 0)) / totalOrig) * 100;
  const pOtro = Math.max(0, 100 - (pCaja + pWeb + pCore));

  document.getElementById('segCaja').style.width = pCaja + '%';
  document.getElementById('segWeb').style.width = pWeb + '%';
  document.getElementById('segCore').style.width = pCore + '%';
  document.getElementById('segOtro').style.width = pOtro + '%';

  document.getElementById('lblCaja').textContent = po.Caja || 0;
  document.getElementById('lblWeb').textContent = po.Web || 0;
  document.getElementById('lblCore').textContent = (po.PanelSoporte || 0) + (po.Core || 0);
  document.getElementById('lblOtro').textContent = Math.max(0, totalOrig - ((po.Caja || 0) + (po.Web || 0) + (po.PanelSoporte || 0) + (po.Core || 0)));

  todosLosLogs = d.logs || [];
  filtrarLocal();
}

function filtrarLocal() {
  const query = (document.getElementById('buscarTexto').value || '').toLowerCase().trim();
  const filtrados = query
    ? todosLosLogs.filter(l =>
        (l.mensaje && l.mensaje.toLowerCase().includes(query)) ||
        (l.logger && l.logger.toLowerCase().includes(query)) ||
        (l.excepcion && l.excepcion.toLowerCase().includes(query)) ||
        (l.origen && l.origen.toLowerCase().includes(query)))
    : todosLosLogs;

  const tbody = document.getElementById('tablaLogs');
  if (filtrados.length === 0) {
    tbody.innerHTML = '<tr><td colspan="5" style="text-align:center;padding:24px;color:#94a3b8;">No hay registros coincidentes.</td></tr>';
    return;
  }

  tbody.innerHTML = filtrados.map(l => `
    <tr>
      <td style="white-space:nowrap;">${new Date(l.fecha).toLocaleString([], { dateStyle:'short', timeStyle:'medium' })}</td>
      <td><span class="nivel nivel-${esc(l.nivel)}">${esc(l.nivel)}</span></td>
      <td>${esc(l.origen || 'Core')}</td>
      <td class="mono" style="color:#94a3b8;font-size:11px;">${esc(l.logger)}</td>
      <td class="mono">
        <div>${esc(l.mensaje)}</div>
        ${l.excepcion ? `<div style="margin-top:4px;padding:6px;background:#1e293b;border-radius:4px;color:#fca5a5;font-size:11px;">${esc(l.excepcion)}</div>` : ''}
      </td>
    </tr>
  `).join('');
}

function exportarLogsCSV() {
  if (!todosLosLogs.length) return;
  const limpiar = s => (s === null || s === undefined ? '' : String(s)).replace(/"/g, '""');
  const lineas = [
    `"Fecha","Nivel","Origen","Logger","Mensaje","Excepcion"`,
    ...todosLosLogs.map(l =>
      `"${limpiar(new Date(l.fecha).toISOString())}","${limpiar(l.nivel)}","${limpiar(l.origen || 'Core')}","${limpiar(l.logger)}","${limpiar(l.mensaje)}","${limpiar(l.excepcion)}"`
    )
  ];
  const blob = new Blob(["\uFEFF" + lineas.join("\r\n")], { type: "text/csv;charset=utf-8;" });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = `logs-core-${new Date().toISOString().slice(0, 10)}.csv`;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);
}
</script>
</body>
</html>
""";
    }
}
