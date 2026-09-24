using System;
using System.Collections.Generic;
using System.Text;

namespace Patronato.Core.Enums
{
    public enum EstadoPrestamo
    {
        Activo = 1,       // Préstamo vigente con cuotas pendientes por pagar
        Pagado = 2,       // Deuda saldada en su totalidad por el beneficiario
        EnMora = 3,       // Atraso en los pagos del puesto
        Exonerado = 4     // Deuda perdonada por el Patronato debido al buen comportamiento
    }
}
