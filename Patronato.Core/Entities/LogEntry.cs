using System;

namespace Patronato.Core.Entities
{
    // Tabla destino del AdoNetAppender de log4net (ver log4net.config).
    public class LogEntry
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Nivel { get; set; } = "";
        public string Origen { get; set; } = "";
        public string Logger { get; set; } = "";
        public string Mensaje { get; set; } = "";
        public string? Excepcion { get; set; }
    }
}
