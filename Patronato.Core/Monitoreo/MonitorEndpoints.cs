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
                var baseDatosOk = true;
                try { await db.Database.ExecuteSqlRawAsync("SELECT 1"); }
                catch { baseDatosOk = false; }

                var consultaLogs = db.Logs.AsQueryable();
                if (!string.IsNullOrWhiteSpace(nivel)) consultaLogs = consultaLogs.Where(l => l.Nivel == nivel);
                if (!string.IsNullOrWhiteSpace(origen)) consultaLogs = consultaLogs.Where(l => l.Origen == origen);

                var logs = await consultaLogs
                    .OrderByDescending(l => l.Fecha)
                    .Take(Math.Clamp(limite ?? 100, 1, 500))
                    .Select(l => new { l.Id, l.Fecha, l.Nivel, l.Origen, l.Logger, l.Mensaje, l.Excepcion })
                    .ToListAsync();

                var ultimaHora = await db.Logs.Where(l => l.Fecha >= haceUnaHora).ToListAsync();

                var resultado = new
                {
                    horaServidor = DateTime.Now,
                    baseDatosOk,
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
                        errores = ultimaHora.Count(l => l.Nivel == "ERROR"),
                        warnings = ultimaHora.Count(l => l.Nivel == "WARN"),
                        total = ultimaHora.Count,
                        porOrigen = ultimaHora.GroupBy(l => l.Origen).ToDictionary(g => g.Key, g => g.Count()),
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
<title>Monitor — Patronato.Core</title>
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
  main { padding: 20px; max-width: 1200px; margin: 0 auto; }
  .login { max-width: 360px; margin: 80px auto; background: #111827; border: 1px solid #1f2937; border-radius: 10px; padding: 24px; text-align: center; }
  .login input { width: 100%; padding: 10px; margin: 8px 0; border-radius: 6px; border: 1px solid #334155; background: #0b1120; color: #e2e8f0; }
  .login button, .toolbar button { background: #2563eb; color: white; border: none; padding: 9px 16px; border-radius: 6px; cursor: pointer; font-size: 13px; }
  .login button:hover, .toolbar button:hover { background: #1d4ed8; }
  .oculto { display: none; }
  .tarjetas { display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); gap: 12px; margin-bottom: 20px; }
  .tarjeta { background: #111827; border: 1px solid #1f2937; border-radius: 10px; padding: 14px; }
  .tarjeta span.etiqueta { display: block; font-size: 11px; color: #94a3b8; text-transform: uppercase; letter-spacing: .04em; }
  .tarjeta span.valor { display: block; font-size: 26px; font-weight: 700; margin-top: 4px; }
  .tarjeta.rojo span.valor { color: #f87171; }
  .tarjeta.ambar span.valor { color: #fbbf24; }
  .panel { background: #111827; border: 1px solid #1f2937; border-radius: 10px; padding: 16px; }
  .panel h2 { margin: 0 0 12px; font-size: 14px; }
  .toolbar { display: flex; gap: 8px; margin-bottom: 12px; flex-wrap: wrap; align-items: center; }
  .toolbar select { background: #0b1120; color: #e2e8f0; border: 1px solid #334155; border-radius: 6px; padding: 7px 10px; font-size: 12px; }
  .toolbar .auto { font-size: 12px; color: #94a3b8; margin-left: auto; }
  table { width: 100%; border-collapse: collapse; font-size: 12px; }
  th { text-align: left; padding: 8px; border-bottom: 1px solid #1f2937; color: #94a3b8; font-weight: 600; }
  td { padding: 8px; border-bottom: 1px solid #1a2332; vertical-align: top; }
  .nivel { padding: 2px 8px; border-radius: 10px; font-size: 11px; font-weight: 700; }
  .nivel-ERROR { background: #450a0a; color: #f87171; }
  .nivel-WARN { background: #451a03; color: #fbbf24; }
  .nivel-INFO { background: #0c2d48; color: #60a5fa; }
  .mono { font-family: ui-monospace, Consolas, monospace; white-space: pre-wrap; }
</style>
</head>
<body>

<div id="login" class="login">
  <h2>🔒 Monitor del Core</h2>
  <p style="color:#94a3b8;font-size:13px;">Acceso directo de soporte/mantenimiento. Inicia sesión con tu usuario del Core.</p>
  <input id="usuario" placeholder="Usuario" autocomplete="username" />
  <input id="clave" type="password" placeholder="Contraseña" autocomplete="current-password" />
  <button onclick="conectar()">Entrar</button>
  <p id="loginError" style="color:#f87171;font-size:12px;"></p>
</div>

<div id="app" class="oculto">
  <header>
    <div>
      <h1>Patronato.Core · Monitor</h1>
      <small>Acceso directo, fuera del Gateway — solo soporte/mantenimiento</small>
    </div>
    <div class="estado">
      <a href="/monitor/operaciones" style="color:#60a5fa;text-decoration:none;font-size:13px;margin-right:16px;">⚙ Operaciones →</a>
      <span id="sesionTexto" style="color:#94a3b8;font-size:12px;margin-right:12px;"></span>
      <button onclick="cerrarSesion()" style="background:#374151;font-size:12px;padding:6px 10px;">Salir</button>
      <span id="puntoEstado" class="punto"></span>
      <span id="textoEstado">Conectando...</span>
    </div>
  </header>

  <main>
    <div class="tarjetas">
      <div class="tarjeta rojo"><span class="etiqueta">Errores (última hora)</span><span class="valor" id="vErrores">—</span></div>
      <div class="tarjeta ambar"><span class="etiqueta">Warnings (última hora)</span><span class="valor" id="vWarnings">—</span></div>
      <div class="tarjeta"><span class="etiqueta">Logs (última hora)</span><span class="valor" id="vTotalHora">—</span></div>
      <div class="tarjeta"><span class="etiqueta">Beneficiarios</span><span class="valor" id="vBeneficiarios">—</span></div>
      <div class="tarjeta"><span class="etiqueta">Transacciones de Caja</span><span class="valor" id="vTransacciones">—</span></div>
      <div class="tarjeta"><span class="etiqueta">Sucursales</span><span class="valor" id="vSucursales">—</span></div>
      <div class="tarjeta"><span class="etiqueta">Préstamos</span><span class="valor" id="vPrestamos">—</span></div>
      <div class="tarjeta"><span class="etiqueta">Por origen (1h) — Caja / Web</span><span class="valor" id="vOrigen" style="font-size:18px;">—</span></div>
    </div>

    <div class="panel">
      <h2>Actividad reciente (tabla Logs)</h2>
      <div class="toolbar">
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
          <option value="Desconocido">Desconocido</option>
        </select>
        <button onclick="cargar()">Refrescar ahora</button>
        <label class="auto"><input type="checkbox" id="autoRefresh" checked onchange="toggleAuto()" /> Auto-refrescar cada 5s</label>
      </div>
      <table>
        <thead><tr><th>Hora</th><th>Nivel</th><th>Origen</th><th>Logger</th><th>Mensaje</th></tr></thead>
        <tbody id="tablaLogs"></tbody>
      </table>
    </div>
  </main>
</div>

<script>
let token = sessionStorage.getItem('monitorToken') || '';
let timer = null;

if (token) conectar(true);

async function conectar(silencioso) {
  if (!silencioso) {
    const usuario = document.getElementById('usuario').value.trim();
    const clave = document.getElementById('clave').value;
    if (!usuario || !clave) return;
    const r = await fetch('/monitor/login', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ nombreUsuario: usuario, password: clave }) });
    const datos = await r.json().catch(() => ({}));
    if (!r.ok) {
      document.getElementById('loginError').textContent = datos.mensaje || 'No se pudo iniciar sesión.';
      return;
    }
    token = datos.token;
    sessionStorage.setItem('monitorToken', token);
    sessionStorage.setItem('monitorSesion', JSON.stringify({ nombre: datos.nombre, rol: datos.rol }));
  }

  fetch('/monitor/datos', { headers: { 'Authorization': 'Bearer ' + token } }).then(async r => {
    if (r.status === 401 || r.status === 403) {
      sessionStorage.removeItem('monitorToken');
      document.getElementById('loginError').textContent = 'Sesión inválida o sin acceso. Inicia sesión de nuevo.';
      return;
    }
    const sesion = JSON.parse(sessionStorage.getItem('monitorSesion') || '{}');
    document.getElementById('sesionTexto').textContent = sesion.nombre ? `${sesion.nombre} (${sesion.rol})` : '';
    document.getElementById('login').classList.add('oculto');
    document.getElementById('app').classList.remove('oculto');
    pintar(await r.json());
    toggleAuto();
  }).catch(() => { document.getElementById('loginError').textContent = 'No se pudo conectar al Core.'; });
}

function cerrarSesion() {
  sessionStorage.removeItem('monitorToken');
  sessionStorage.removeItem('monitorSesion');
  location.reload();
}

function cargar() {
  const nivel = document.getElementById('filtroNivel').value;
  const origen = document.getElementById('filtroOrigen').value;
  const qs = new URLSearchParams({ nivel, origen, limite: 150 });
  fetch('/monitor/datos?' + qs, { headers: { 'Authorization': 'Bearer ' + token } }).then(async r => {
    if (r.status === 401 || r.status === 403) { clearInterval(timer); cerrarSesion(); return; }
    pintar(await r.json());
  });
}

function toggleAuto() {
  clearInterval(timer);
  if (document.getElementById('autoRefresh').checked) timer = setInterval(cargar, 5000);
}

function pintar(d) {
  document.getElementById('puntoEstado').className = 'punto ' + (d.baseDatosOk ? 'ok' : 'mal');
  document.getElementById('textoEstado').textContent = d.baseDatosOk
    ? 'Base de datos OK · ' + new Date(d.horaServidor).toLocaleTimeString()
    : 'Base de datos NO responde';

  document.getElementById('vErrores').textContent = d.ultimaHora.errores;
  document.getElementById('vWarnings').textContent = d.ultimaHora.warnings;
  document.getElementById('vTotalHora').textContent = d.ultimaHora.total;
  document.getElementById('vBeneficiarios').textContent = d.conteos.beneficiarios;
  document.getElementById('vTransacciones').textContent = d.conteos.transaccionesCaja;
  document.getElementById('vSucursales').textContent = d.conteos.sucursales;
  document.getElementById('vPrestamos').textContent = d.conteos.prestamos;
  const po = d.ultimaHora.porOrigen || {};
  document.getElementById('vOrigen').textContent = (po.Caja || 0) + ' / ' + (po.Web || 0);

  const tbody = document.getElementById('tablaLogs');
  tbody.innerHTML = d.logs.map(l => `
    <tr>
      <td>${new Date(l.fecha).toLocaleString()}</td>
      <td><span class="nivel nivel-${l.nivel}">${l.nivel}</span></td>
      <td>${l.origen ?? ''}</td>
      <td class="mono">${l.logger ?? ''}</td>
      <td class="mono">${(l.mensaje ?? '') + (l.excepcion ? '\\n' + l.excepcion : '')}</td>
    </tr>
  `).join('');
}
</script>
</body>
</html>
""";
    }
}
