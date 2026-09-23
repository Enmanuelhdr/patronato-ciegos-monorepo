using Patronato.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Patronato.Core.Entities
{
    public class PrestamoEmprendimiento
    {
        public int Id { get; set; }

        // Relación con el Beneficiario
        public int BeneficiarioId { get; set; }
        public Beneficiario? Beneficiario { get; set; }
        // Datos del crédito
        public decimal MontoAprobado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public int PlazoMeses { get; set; }
        public RubroMicroemprendimiento Rubro { get; set; }

        // Condiciones institucionales del Patronato
        public bool EstudioFactibilidadAprobado { get; set; }
        public bool InvolucraFamilia { get; set; }
        public EstadoPrestamo Estado { get; set; } = EstadoPrestamo.Activo;
        public string JustificacionExoneracion { get; set; } = string.Empty;
        // Regla de Negocio: Pago de cuota desde Caja
        public void RegistrarAbono(decimal monto)
        {
            if (Estado != EstadoPrestamo.Activo)
                throw new InvalidOperationException("El préstamo no está activo para recibir abonos.");

            if (monto <= 0)
                throw new ArgumentException("El monto debe ser mayor a cero.");
            SaldoPendiente -= monto;
            if (SaldoPendiente <= 0)
            {
                SaldoPendiente = 0;
                Estado = EstadoPrestamo.Pagado;
            }
        }
        // Regla de Negocio: Exoneración por buen comportamiento
        public void ExonerarDeuda(string justificacion)
        {
            if (string.IsNullOrWhiteSpace(justificacion))
                throw new ArgumentException("Se requiere la justificación del comité directivo para exonerar.");
            SaldoPendiente = 0;
            Estado = EstadoPrestamo.Exonerado;
            JustificacionExoneracion = justificacion;
        }
    }

}
