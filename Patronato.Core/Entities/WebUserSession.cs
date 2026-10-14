using System;

namespace Patronato.Core.Entities
{
    // Sesión (token) emitida a un WebUser. Permite que el Core "fuerce el cierre de
    // sesión" marcando RevocadoEn: una vez exista el login público real, su validación
    // deberá rechazar cualquier Jti que aparezca aquí revocado.
    public class WebUserSession
    {
        public int Id { get; set; }

        public int WebUserId { get; set; }
        public WebUser? WebUser { get; set; }

        public string Jti { get; set; } = string.Empty;
        public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
        public DateTime? RevocadoEn { get; set; }
    }
}
