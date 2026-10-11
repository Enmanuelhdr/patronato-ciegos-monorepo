using System;
using Patronato.Caja.Models;

namespace Patronato.Caja.Services
{
    public static class SessionManager
    {
        public static UsuarioSession? CurrentSession { get; set; }

        // La Caja nunca habla directo con el Core: todo pasa por el Gateway (ver
        // Patronato.Gateway). Las consultas van por HTTP (reenviadas al Core) y las
        // transacciones de dinero van por el socket de códigos (GatewaySocketClient).
        public static string BaseApiUrl { get; set; } = "http://localhost:57171";

        public static int GatewaySocketPort { get; set; } = 6000;

        public static bool IsLoggedIn => CurrentSession != null;

        public static (string Host, int Port) GetGatewaySocketEndpoint()
        {
            var host = "localhost";
            try
            {
                host = new Uri(BaseApiUrl).Host;
            }
            catch (UriFormatException)
            {
                // Se mantiene "localhost" si el usuario escribió una URL inválida en el login.
            }

            return (host, GatewaySocketPort);
        }

        public static void SetSession(UsuarioSession session)
        {
            CurrentSession = session;
        }

        public static void Logout()
        {
            CurrentSession = null;
        }
    }
}
