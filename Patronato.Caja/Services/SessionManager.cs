using System;
using Patronato.Caja.Models;

namespace Patronato.Caja.Services
{
    public static class SessionManager
    {
        public static UsuarioSession? CurrentSession { get; set; }
        public static string BaseApiUrl { get; set; } = "http://localhost:57141";

        public static bool IsLoggedIn => CurrentSession != null;

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
