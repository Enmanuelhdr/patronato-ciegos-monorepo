using System;
using System.Collections.Generic;
using System.Text;

namespace Patronato.Core.Enums
{
    public enum RolUsuario
    {
        Administrador = 1,              // Acceso total al sistema
        Cajero = 2,                     // Solo opera la Caja y cobros
        MedicoOftalmologo = 3,          // Solo atiende consultas de prevención
        InstructorRehabilitacion = 4,   // Solo gestiona los talleres (Braille, bastón)
        Recepcionista = 5               // Registra pacientes y agenda citas
    }
}
