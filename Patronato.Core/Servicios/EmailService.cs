using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Patronato.Core.Servicios
{
    public interface IEmailService
    {
        Task<(bool Enviado, string? Mensaje)> EnviarCodigoVerificacionAsync(string correoDestino, string nombreDestinatario, string codigoOtp);
        Task<(bool Enviado, string? Mensaje)> EnviarEmailAsync(string correoDestino, string asunto, string cuerpoHtml);
    }

    public class EmailService : IEmailService
    {
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(EmailService));
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<(bool Enviado, string? Mensaje)> EnviarCodigoVerificacionAsync(string correoDestino, string nombreDestinatario, string codigoOtp)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n=======================================================");
            Console.WriteLine($"[PATRONATO - SEGURIDAD OTP] Código emitido para: {correoDestino}");
            Console.WriteLine($"PIN: {codigoOtp}");
            Console.WriteLine($"=======================================================\n");
            Console.ResetColor();

            Logger.Info($"[PATRONATO - SEGURIDAD OTP] Código emitido para '{correoDestino}': {codigoOtp}");

            var asunto = "Código de Verificación - Patronato Nacional de Ciegos";
            var cuerpoHtml = $@"
<!DOCTYPE html>
<html lang='es'>
<head>
  <meta charset='UTF-8'>
  <title>Verificación de Correo - Patronato Nacional de Ciegos</title>
</head>
<body style='margin:0;padding:24px 0;background-color:#0f172a;font-family:-apple-system,BlinkMacSystemFont,""Segoe UI"",Roboto,sans-serif;'>
  <table align='center' border='0' cellpadding='0' cellspacing='0' width='100%' style='max-width:560px;margin:0 auto;background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 20px 40px rgba(0,0,0,0.3);'>
    <!-- Header Institucional -->
    <tr>
      <td style='background:#1e3a8a;padding:28px 32px;text-align:center;'>
        <div style='display:inline-block;background:#ffffff;color:#1e3a8a;font-weight:900;font-size:18px;letter-spacing:1px;padding:6px 16px;border-radius:8px;margin-bottom:10px;'>PNC</div>
        <h1 style='margin:0;color:#ffffff;font-size:18px;font-weight:800;letter-spacing:0.5px;'>PATRONATO NACIONAL DE CIEGOS, INC.</h1>
        <p style='margin:4px 0 0;color:#93c5fd;font-size:12px;'>Portal Institucional de Servicios y Pacientes</p>
      </td>
    </tr>
    <!-- Contenido -->
    <tr>
      <td style='padding:36px 32px;color:#1e293b;'>
        <h2 style='margin:0 0 12px;font-size:20px;color:#0f172a;font-weight:800;'>Verifica tu Correo Electrónico</h2>
        <p style='margin:0 0 18px;font-size:14px;line-height:1.6;color:#475569;'>
          Hola <strong>{WebUtility.HtmlEncode(nombreDestinatario)}</strong>,
        </p>
        <p style='margin:0 0 24px;font-size:14px;line-height:1.6;color:#475569;'>
          Has solicitado registrarte en el <strong>Portal de Pacientes del Patronato Nacional de Ciegos</strong>. Para confirmar que este correo te pertenece e iniciar sesión en tu cuenta, introduce el siguiente código PIN de seguridad de 6 dígitos:
        </p>

        <!-- Código PIN en grande -->
        <div style='background:#f1f5f9;border:2px dashed #0284c7;border-radius:12px;padding:20px;text-align:center;margin:0 0 26px;'>
          <span style='font-size:11px;text-transform:uppercase;letter-spacing:1px;font-weight:700;color:#0284c7;display:block;margin-bottom:6px;'>Tu Código PIN de 6 Dígitos</span>
          <span style='font-size:36px;font-weight:900;letter-spacing:10px;color:#1e3a8a;font-family:Consolas,monospace;'>{codigoOtp}</span>
          <span style='display:block;font-size:12px;color:#64748b;margin-top:6px;'>Válido durante los próximos 15 minutos</span>
        </div>

        <p style='margin:0 0 12px;font-size:13px;line-height:1.5;color:#64748b;'>
          🔒 <strong>Por tu seguridad:</strong> Nunca compartas este código con ninguna otra persona. El personal del Patronato nunca te pedirá este código por llamada telefónica ni por mensaje directo.
        </p>
        <p style='margin:0;font-size:12px;line-height:1.5;color:#94a3b8;'>
          Si no has solicitado esta cuenta en el Patronato Nacional de Ciegos, puedes desestimar este mensaje; nadie podrá acceder a tu cuenta sin este PIN.
        </p>
      </td>
    </tr>
    <!-- Footer -->
    <tr>
      <td style='background:#f8fafc;padding:20px 32px;text-align:center;border-top:1px solid #e2e8f0;font-size:11px;color:#94a3b8;'>
        <p style='margin:0 0 4px;'>Patronato Nacional de Ciegos, Inc. · RNC: 4-01-00623-1</p>
        <p style='margin:0;'>Calle Huáscar Tejeda #54, Zona Universitaria, Santo Domingo, R.D. · Tel: (809) 533-2833</p>
      </td>
    </tr>
  </table>
</body>
</html>";

            return await EnviarEmailAsync(correoDestino, asunto, cuerpoHtml);
        }

        public async Task<(bool Enviado, string? Mensaje)> EnviarEmailAsync(string correoDestino, string asunto, string cuerpoHtml)
        {
            var host = _config["Smtp:Host"] ?? "smtp.gmail.com";
            var portStr = _config["Smtp:Port"] ?? "587";
            int.TryParse(portStr, out int port);
            if (port <= 0) port = 587;

            var enableSsl = bool.Parse(_config["Smtp:EnableSsl"] ?? "true");
            var usuario = _config["Smtp:Usuario"]?.Trim() ?? "";
            var password = _config["Smtp:Password"]?.Trim() ?? "";
            var remitente = _config["Smtp:Remitente"]?.Trim();
            if (string.IsNullOrWhiteSpace(remitente)) remitente = !string.IsNullOrWhiteSpace(usuario) ? usuario : "notificaciones@patronatociegos.org.do";
            var nombreRemitente = _config["Smtp:NombreRemitente"] ?? "Patronato Nacional de Ciegos";

            // Si no hay credenciales SMTP configuradas, registrar en bitácora
            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(password))
            {
                var msgNoSmtp = $"[SMTP] No se han configurado credenciales (Smtp:Usuario / Smtp:Password en appsettings.json). Para enviar correos reales a {correoDestino}, ingrese su cuenta emisora y contraseña de aplicación en appsettings.json.";
                Logger.Warn(msgNoSmtp);
                return (false, msgNoSmtp);
            }

            try
            {
                using var mensaje = new MailMessage
                {
                    From = new MailAddress(remitente, nombreRemitente, Encoding.UTF8),
                    Subject = asunto,
                    Body = cuerpoHtml,
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                };
                mensaje.To.Add(new MailAddress(correoDestino));

                using var smtp = new SmtpClient(host, port)
                {
                    EnableSsl = enableSsl,
                    Credentials = new NetworkCredential(usuario, password),
                    Timeout = 12000
                };

                await smtp.SendMailAsync(mensaje);
                Logger.Info($"Correo electrónico enviado exitosamente a '{correoDestino}' (Asunto: '{asunto}').");
                return (true, "Correo enviado exitosamente.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error al enviar correo electrónico a '{correoDestino}' vía {host}:{port}.", ex);
                return (false, $"Error al conectar con el servidor de correo: {ex.Message}");
            }
        }
    }
}
