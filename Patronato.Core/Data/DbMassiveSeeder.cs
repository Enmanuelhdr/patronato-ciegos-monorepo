using System;
using System.Collections.Generic;
using System.Linq;
using Patronato.Core.Entities;
using Patronato.Core.Enums;

namespace Patronato.Core.Data
{
    public static class DbMassiveSeeder
    {
        public static (int Nuevos, string Detalle) SembrarDatosSiFaltan(ApplicationDbContext context)
        {
            int totalAgregados = 0;
            var resumen = new List<string>();

            // 1. PRODUCTOS / ARTÍCULOS (Garantizar mínimo 45)
            if (context.ProductosArticulos.Count() < 40)
            {
                var productosExistentes = context.ProductosArticulos.Select(p => p.Codigo).ToHashSet();
                var catalogo = new (string Codigo, string Nombre, string Categoria, decimal Precio, int Stock, int Minimo)[]
                {
                    // Movilidad e Independencia
                    ("BAST-01", "Bastón Blanco Plegable en Aluminio (4 tramos)", "Movilidad", 650m, 35, 10),
                    ("BAST-02", "Bastón Verde Plegable (Baja Visión)", "Movilidad", 750m, 20, 8),
                    ("BAST-03", "Bastón Rojo y Blanco (Sordoceguera)", "Movilidad", 800m, 15, 5),
                    ("BAST-04", "Bastón Rígido de Grafito Ultraligero", "Movilidad", 1200m, 12, 4),
                    ("BAST-05", "Puntero Rollerball Giratorio de Repuesto", "Accesorios", 280m, 50, 15),
                    ("BAST-06", "Puntero de Cerámica Tipo Lápiz", "Accesorios", 220m, 40, 12),
                    ("BAST-07", "Puntero Tipo Bellota en Nylon Alta Resistencia", "Accesorios", 190m, 45, 10),
                    ("BAST-08", "Correa Elástica de Muñeca para Bastón", "Accesorios", 95m, 60, 20),

                    // Tiflotecnología y Lectoescritura Braille
                    ("BRAI-01", "Regleta Braille de Aluminio (4 Líneas x 28 Celdas)", "Tiflotecnología", 450m, 30, 8),
                    ("BRAI-02", "Regleta Braille de Bolsillo Plástica (2 Líneas x 28)", "Tiflotecnología", 250m, 40, 10),
                    ("BRAI-03", "Punzón Anatómico Ergonómico de Acero", "Tiflotecnología", 150m, 55, 15),
                    ("BRAI-04", "Punzón Tipo Sillero Mango de Madera", "Tiflotecnología", 175m, 30, 10),
                    ("BRAI-05", "Papel Ledger Especial para Braille (Resma 100 Hojas)", "Papelería", 350m, 25, 5),
                    ("BRAI-06", "Cuaderno Pautado de Escritura en Relieve", "Papelería", 220m, 35, 8),
                    ("BRAI-07", "Cinta Rotuladora Dymo Braille Adhesiva", "Tiflotecnología", 180m, 40, 10),
                    ("BRAI-08", "Rotuladora Manual Dymo Adaptada con Disco Braille", "Tiflotecnología", 1450m, 10, 3),
                    ("BRAI-09", "Calculadora Parlante Científica en Español", "Tecnología", 1850m, 8, 3),
                    ("BRAI-10", "Reloj Parlante Digital de Muñeca (Alarma y Hora en Español)", "Tecnología", 650m, 28, 8),
                    ("BRAI-11", "Reloj Táctil Analógico con Bisel Abatible de Acero", "Tecnología", 950m, 18, 5),
                    ("BRAI-12", "Termómetro Clínico Digital Parlante", "Salud", 550m, 22, 6),
                    ("BRAI-13", "Detector Sonoro de Nivel de Líquidos para Tazas", "Accesibilidad", 320m, 30, 8),
                    ("BRAI-14", "Identificador Parlante de Colores y Luz", "Tecnología", 2800m, 5, 2),

                    // Salud Visual, Óptica y Protección
                    ("OPT-01", "Lupa Electrónica Digital Portátil HD con Pantalla 4.3\"", "Óptica", 3900m, 6, 2),
                    ("OPT-02", "Lupa Manual Rectangular con Luz LED 5X", "Óptica", 650m, 25, 8),
                    ("OPT-03", "Lupa Tipo Domo de Cristal de Lectura 4X", "Óptica", 780m, 16, 5),
                    ("OPT-04", "Filtro Ocular Terapéutico Amarillo Antirreflejo", "Óptica", 850m, 20, 5),
                    ("OPT-05", "Filtro Ocular Terapéutico Ámbar Alto Contraste", "Óptica", 890m, 18, 5),
                    ("OPT-06", "Gafas de Sol Envolventes Polarizadas con Protección UV400", "Óptica", 600m, 30, 10),
                    ("OPT-07", "Gotas Lubricantes Lágrimas Artificiales Estériles (15 ml)", "Salud", 290m, 40, 12),
                    ("OPT-08", "Solución Salina Fisiológica Ocular (Envase 250 ml)", "Salud", 180m, 35, 10),
                    ("OPT-09", "Parches Oculares Ortopédicos Hipoalergénicos (Caja 30 uds)", "Salud", 320m, 25, 6),

                    // Productos Ecológicos y Taller Protegido (Eco-friendly)
                    ("ECO-01", "Suaper Institucional de Algodón Reciclado (Mango Madera)", "Limpieza", 260m, 60, 15),
                    ("ECO-02", "Suaper Industrial Pesado Fibras Biodegradables", "Limpieza", 390m, 40, 12),
                    ("ECO-03", "Trapeador Ecológico de Microfibra Lavable", "Limpieza", 210m, 50, 15),
                    ("ECO-04", "Portavasos de Macramé Tejidos por Beneficiarios (Set de 4)", "Artesanías", 250m, 30, 8),
                    ("ECO-05", "Canasta Organizadora de Yute y Fibras Naturales", "Artesanías", 420m, 20, 5),
                    ("ECO-06", "Bolsa Ecológica Reutilizable de Tela con Logo Bordado", "Artesanías", 180m, 75, 20),
                    ("ECO-07", "Jabón Artesanal de Avena y Miel Orgánica", "Artesanías", 120m, 60, 15),
                    ("ECO-08", "Vela Aromática de Cera de Soya en Envase Reciclado", "Artesanías", 220m, 35, 10),
                    ("ECO-09", "Desinfectante Multiusos Ecológico Lavanda (1 Galón)", "Limpieza", 320m, 45, 12),
                    ("ECO-10", "Cloro Ecológico Bajo Impacto Ambiental (1 Galón)", "Limpieza", 210m, 50, 15),
                    ("ECO-11", "Balón Sonoro de Goalball con Cascabeles Internos", "Deportes", 1850m, 14, 4),
                    ("ECO-12", "Juego de Ajedrez Táctil Adaptado con Relieves", "Juegos", 1450m, 12, 3),
                    ("ECO-13", "Dominó Adaptado Táctil con Puntos Marcados", "Juegos", 750m, 20, 5),
                    ("ECO-14", "Baraja Española Táctil Marcada en Braille", "Juegos", 420m, 28, 6),
                };

                int agregados = 0;
                foreach (var p in catalogo)
                {
                    if (!productosExistentes.Contains(p.Codigo))
                    {
                        context.ProductosArticulos.Add(new ProductoArticulo
                        {
                            Codigo = p.Codigo,
                            Nombre = p.Nombre,
                            Categoria = p.Categoria,
                            PrecioVenta = p.Precio,
                            StockDisponible = p.Stock,
                            StockMinimo = p.Minimo,
                            Activo = true
                        });
                        agregados++;
                    }
                }
                context.SaveChanges();
                totalAgregados += agregados;
                resumen.Add($"{agregados} Productos de Catálogo");
            }

            // 2. BENEFICIARIOS (Garantizar mínimo 80)
            if (context.Beneficiarios.Count() < 75)
            {
                var nombresFemeninos = new[] { "Rosa", "Carmen", "Altagracia", "María", "Yudith", "Francisca", "Lourdes", "Claribel", "Ana", "Dinorah", "Yomaira", "Xiomara", "Mercedes", "Patricia", "Lucía", "Dilenia", "Belkis", "Esmeralda", "Fiordaliza", "Wanda" };
                var nombresMasculinos = new[] { "Juan", "Pedro", "Manuel", "Carlos", "José", "Robinson", "Kelvin", "Ramón", "Héctor", "David", "Francisco", "Víctor", "Julio", "Franklin", "Wilfredo", "Alexis", "César", "Orlando", "Braulio", "Elvin" };
                var apellidos = new[] { "Pérez", "Rosario", "Cruz", "Medina", "Morales", "Gómez", "Bautista", "Batista", "Reyes", "Ventura", "Cedeño", "De León", "Peña", "Castillo", "Jiménez", "Valdez", "Pimentel", "Matos", "Encarnación", "Suárez" };

                var rand = new Random(1024);
                int cantidadNecesaria = 80 - context.Beneficiarios.Count();
                int cedulaBase = 1002000;

                for (int i = 0; i < cantidadNecesaria; i++)
                {
                    bool esFemenino = (i % 2 == 0);
                    string nombre = esFemenino 
                        ? nombresFemeninos[rand.Next(nombresFemeninos.Length)] 
                        : nombresMasculinos[rand.Next(nombresMasculinos.Length)];
                    string apellido1 = apellidos[rand.Next(apellidos.Length)];
                    string apellido2 = apellidos[rand.Next(apellidos.Length)];

                    int prefijo = i % 3 == 0 ? 1 : (i % 3 == 1 ? 31 : 18);
                    string cedula = $"{prefijo:D3}-{(cedulaBase + i):D7}-{(i % 9) + 1}";
                    string telefono = $"809-555-{(1000 + i):D4}";

                    var sede = (i % 3) switch
                    {
                        0 => SedeEnum_REVIEW.SantoDomingo,
                        1 => SedeEnum_REVIEW.Santiago,
                        _ => SedeEnum_REVIEW.Barahona
                    };

                    var condicion = (i % 3) switch
                    {
                        0 => TipoDiscapacidadVisual.CegueraTotalCongenita,
                        1 => TipoDiscapacidadVisual.CegueraTotalAdquirida,
                        _ => TipoDiscapacidadVisual.BajaVision
                    };

                    context.Beneficiarios.Add(new Beneficiario
                    {
                        Nombres = nombre,
                        Apellidos = $"{apellido1} {apellido2}",
                        Cedula = cedula,
                        Telefono = telefono,
                        SedeAsignada = sede,
                        CondicionVisual = condicion,
                        Activo = true
                    });
                }
                context.SaveChanges();
                totalAgregados += cantidadNecesaria;
                resumen.Add($"{cantidadNecesaria} Beneficiarios");
            }

            var todosBeneficiarios = context.Beneficiarios.ToList();

            // 3. CONSULTAS OFTALMOLÓGICAS (Garantizar mínimo 85)
            if (context.ConsultasOftalmologicas.Count() < 80 && todosBeneficiarios.Any())
            {
                var doctores = new[]
                {
                    "Dra. Altagracia Valdez (Especialista en Glaucoma)",
                    "Dr. Marcos Rosario (Cirujano de Retina y Vítreo)",
                    "Dra. Claudia Henríquez (Segmento Anterior y Catarata)",
                    "Dr. Kelvin Méndez (Especialista en Baja Visión)",
                    "Dra. Laura Pimentel (Oftalmología Pediátrica)",
                    "Dr. Héctor Bienvenido Cabral (Optometría Clínica)"
                };

                var diagnosticos = new (string Diagnostico, string Tratamiento, decimal Tarifa, bool ConArs)[]
                {
                    ("Glaucoma Primario de Ángulo Abierto", "Prescripción de gotas hipotensoras Latanoprost 0.005%, control tensional en 30 días.", 1200m, true),
                    ("Catarata Senil Nuclear Bilateral Grado III", "Candidato a cirugía de facoemulsificación con implante de lente intraocular.", 1500m, true),
                    ("Retinopatía Diabética Proliferativa con Edema", "Sesión de fotocoagulación con láser argón y derivación a endocrinología.", 1800m, true),
                    ("Degeneración Macular Asociada a la Edad (DMAE)", "Prescripción de suplementos vitamínicos AREDS2 y entrenamiento en ayudas ópticas.", 1200m, false),
                    ("Queratocono Progresivo Grado II", "Adaptación de lentes esclerales y control topográfico corneal semestral.", 1400m, true),
                    ("Astigmatismo Miópico Severo con Ambliopía", "Corrección óptica aérea con lentes monofocales de alto índice y filtro ámbar.", 800m, true),
                    ("Ojo Seco Severo con Blefaritis", "Lágrimas artificiales sin preservantes cada 4h y compresas tibias palpebrales.", 600m, false),
                    ("Atrofia Óptica Bilateral Secundaria", "Remisión a programa de rehabilitación funcional en Orientación y Movilidad.", 900m, true),
                };

                var rand = new Random(2048);
                int agregados = 0;
                int target = 85 - context.ConsultasOftalmologicas.Count();

                for (int i = 0; i < target; i++)
                {
                    var benef = todosBeneficiarios[i % todosBeneficiarios.Count];
                    var diag = diagnosticos[rand.Next(diagnosticos.Length)];
                    var doctor = doctores[rand.Next(doctores.Length)];
                    var fecha = DateTime.UtcNow.AddDays(-rand.Next(1, 90)).AddHours(rand.Next(8, 17));

                    context.ConsultasOftalmologicas.Add(new ConsultaOftalmologica
                    {
                        BeneficiarioId = benef.Id,
                        Sede = benef.SedeAsignada,
                        Oftalmologo = doctor,
                        Fecha = fecha,
                        DiagnosticoOcular = diag.Diagnostico,
                        TratamientoPrescrito = diag.Tratamiento,
                        TarifaConsulta = diag.Tarifa,
                        CubiertoPorARS = diag.ConArs,
                        EstaFacturadaEnCaja = (i % 2 == 0) // Mitad facturadas, mitad listas para cobrar
                    });
                    agregados++;
                }
                context.SaveChanges();
                totalAgregados += agregados;
                resumen.Add($"{agregados} Consultas Oftalmológicas");
            }

            // 4. SESIONES DE MASAJE TACTO (Garantizar mínimo 70)
            if (context.SesionesMasaje.Count() < 65)
            {
                var masajistas = new[]
                {
                    "David Silverio (Terapeuta Invidente)",
                    "Claribel Batista (Terapeuta Invidente)",
                    "Roberto Ventura (Terapeuta Invidente)",
                    "Carmen Lidia Pérez (Terapeuta Invidente)",
                    "Francisco Javier Peña (Terapeuta Invidente)",
                    "Ana Belkis Morales (Terapeuta Invidente)",
                    "Pedro Julio Cedeño (Terapeuta Invidente)",
                    "Yaniris Altagracia Cruz (Terapeuta Invidente)"
                };

                var clientes = new[]
                {
                    ("Ing. Juan Pablo Duarte", "809-555-1122"),
                    ("Licda. Sandra Guzmán", "829-444-2233"),
                    ("Dr. Fernando Taveras", "849-333-4455"),
                    ("Sra. Matilde Henríquez", "809-666-5566"),
                    ("Sr. Rafael Antonio Mieses", "829-777-6677"),
                    ("Arq. Carolina Brea", "849-888-7788"),
                    ("Lic. Marcos Aurelio Peña", "809-999-8899"),
                    ("Dra. Beatriz Polanco", "829-111-9900"),
                    ("Sr. Emilio Germán", "849-222-0011"),
                    ("Sra. Teresa de Jesús Lora", "809-333-1122")
                };

                var opciones = new (int Duracion, decimal Tarifa)[]
                {
                    (30, 500m),
                    (45, 750m),
                    (60, 1000m),
                    (90, 1500m)
                };

                var rand = new Random(4096);
                int target = 70 - context.SesionesMasaje.Count();
                int agregados = 0;

                for (int i = 0; i < target; i++)
                {
                    var masajista = masajistas[i % masajistas.Length];
                    var cliente = clientes[rand.Next(clientes.Length)];
                    var opcion = opciones[rand.Next(opciones.Length)];
                    var fecha = DateTime.Now.AddDays(-rand.Next(0, 45)).AddHours(rand.Next(9, 18));
                    var sede = (i % 3) switch
                    {
                        0 => SedeEnum_REVIEW.SantoDomingo,
                        1 => SedeEnum_REVIEW.Santiago,
                        _ => SedeEnum_REVIEW.Barahona
                    };

                    context.SesionesMasaje.Add(new SesionMasajeTacto
                    {
                        NombreCliente = $"{cliente.Item1} #{i + 1}",
                        TelefonoCliente = cliente.Item2,
                        NombreMasajista = masajista,
                        Sede = sede,
                        FechaHora = fecha,
                        DuracionMinutos = opcion.Duracion,
                        PrecioTarifa = opcion.Tarifa,
                        EstaFacturadoEnCaja = (i % 3 != 0) // Dejar una tercera parte pendiente de facturar para probar Caja!
                    });
                    agregados++;
                }
                context.SaveChanges();
                totalAgregados += agregados;
                resumen.Add($"{agregados} Sesiones de Masaje TACTO");
            }

            // 5. PRÉSTAMOS DE EMPRENDIMIENTO (Garantizar mínimo 40)
            if (context.PrestamosEmprendimiento.Count() < 35 && todosBeneficiarios.Count >= 20)
            {
                var rand = new Random(8192);
                int target = 40 - context.PrestamosEmprendimiento.Count();
                int agregados = 0;

                for (int i = 0; i < target && i < todosBeneficiarios.Count; i++)
                {
                    var benef = todosBeneficiarios[i];
                    var rubro = (RubroMicroemprendimiento)((i % 9) + 1);
                    decimal monto = (rand.Next(2, 10) * 10000m); // 20k a 90k
                    decimal pagado = (i % 4 == 0) ? monto : (monto * (rand.Next(1, 8) / 10m));
                    decimal saldo = monto - pagado;

                    var estado = saldo <= 0 ? EstadoPrestamo.Pagado : EstadoPrestamo.Activo;

                    context.PrestamosEmprendimiento.Add(new PrestamoEmprendimiento
                    {
                        BeneficiarioId = benef.Id,
                        MontoAprobado = monto,
                        SaldoPendiente = saldo,
                        PlazoMeses = (i % 3 == 0) ? 12 : ((i % 3 == 1) ? 18 : 24),
                        Rubro = rubro,
                        EstudioFactibilidadAprobado = true,
                        InvolucraFamilia = (i % 2 == 0),
                        Estado = estado
                    });
                    agregados++;
                }
                context.SaveChanges();
                totalAgregados += agregados;
                resumen.Add($"{agregados} Préstamos de Emprendimiento");
            }

            // 6. DONACIONES (Garantizar mínimo 50)
            if (context.Donaciones.Count() < 45)
            {
                var donantesEmpresas = new[]
                {
                    ("Banco BHD, S.A.", "1-01-00123-4", 150000m, "Transferencia", true),
                    ("Cervecería Nacional Dominicana", "1-01-00234-5", 250000m, "Transferencia", true),
                    ("Grupo Ramos (Sirena / Aprezio)", "1-01-00345-6", 100000m, "Transferencia", true),
                    ("Fundación Brugal", "1-01-00456-7", 80000m, "Transferencia", true),
                    ("Grupo Corripio", "1-01-00567-8", 120000m, "Transferencia", true),
                    ("Banco Popular Dominicano", "1-01-00678-9", 300000m, "Transferencia", true),
                    ("Claro Dominicana", "1-01-00789-0", 75000m, "Transferencia", true),
                    ("Central Romana Corporation", "1-01-00890-1", 180000m, "Transferencia", true),
                    ("Industrias San Miguel (Kola Real)", "1-01-00901-2", 60000m, "Transferencia", true),
                    ("Asociación Popular de Ahorros y Préstamos", "1-01-01012-3", 95000m, "Transferencia", true)
                };

                var donantesParticulares = new[]
                {
                    ("Familia González Vicini", "001-1020304-5", 25000m, "Transferencia", false),
                    ("Dr. Alejandro Asmar", "001-2030405-6", 15000m, "Tarjeta", false),
                    ("Dra. Miriam Cabral", "001-3040506-7", 10000m, "Tarjeta", false),
                    ("Ing. Federico Antún", "001-4050607-8", 12500m, "Transferencia", false),
                    ("Lic. Bernardo Vega", "001-5060708-9", 8000m, "Efectivo", false),
                    ("Anónimo Solidario Zona Universitaria", "001-6070809-0", 5000m, "Efectivo", false),
                    ("Club de Leones Santo Domingo Central", "4-01-99887-1", 35000m, "Transferencia", true),
                    ("Club Rotario Santo Domingo Bella Vista", "4-01-88776-2", 40000m, "Transferencia", true),
                    ("Voluntariado Banreservas", "4-01-77665-3", 65000m, "Transferencia", true),
                    ("Sra. Carmenchu Rivera", "001-7080910-1", 6000m, "Tarjeta", false)
                };

                var rand = new Random(16384);
                int target = 50 - context.Donaciones.Count();
                int agregados = 0;

                for (int i = 0; i < target; i++)
                {
                    bool esEmpresa = (i % 2 == 0);
                    var donor = esEmpresa 
                        ? donantesEmpresas[rand.Next(donantesEmpresas.Length)] 
                        : donantesParticulares[rand.Next(donantesParticulares.Length)];

                    var fecha = DateTime.UtcNow.AddDays(-rand.Next(1, 120));
                    decimal monto = donor.Item3 + (rand.Next(0, 5) * 1000m);

                    context.Donaciones.Add(new Donacion
                    {
                        Donante = donor.Item1,
                        RncCedula = donor.Item2,
                        Monto = monto,
                        Fecha = fecha,
                        MetodoPago = donor.Item4,
                        RequiereComprobanteFiscal = donor.Item5,
                        EstaRegistradaEnCaja = true
                    });
                    agregados++;
                }
                context.SaveChanges();
                totalAgregados += agregados;
                resumen.Add($"{agregados} Donaciones Solidarias");
            }

            // 7. INSCRIPCIONES EN PROGRAMAS DE REHABILITACIÓN (Garantizar mínimo 35)
            if (context.InscripcionesRehabilitacion.Count() < 30 && todosBeneficiarios.Any())
            {
                var talleres = new[]
                {
                    "Orientación y Movilidad con Bastón Blanco",
                    "Sistema de Lectoescritura Braille Inicial",
                    "Braille Avanzado y Signografía Matemática",
                    "Tiflotecnología y Manejo de Lector de Pantalla NVDA",
                    "Actividades de la Vida Diaria (Cocina, Aseo y Autonomía)",
                    "Centro de Masajes TACTO (Capacitación Profesional en Masoterapia)",
                    "Taller Eco-Artesanal: Tejido de Macramé y Reciclaje Creativo",
                    "Elaboración Ecológica de Detergentes y Suapers Institucionales",
                    "Ajedrez Táctil y Estimulación Cognitiva"
                };

                var rand = new Random(32768);
                int target = 35 - context.InscripcionesRehabilitacion.Count();
                int agregados = 0;

                for (int i = 0; i < target; i++)
                {
                    var benef = todosBeneficiarios[(i + 5) % todosBeneficiarios.Count];
                    var taller = talleres[rand.Next(talleres.Length)];
                    var etapa = (i % 2 == 0) ? EtapaRehabilitacion.Funcional : EtapaRehabilitacion.Profesional;
                    var fecha = DateTime.UtcNow.AddMonths(-rand.Next(1, 12));

                    context.InscripcionesRehabilitacion.Add(new InscripcionRehabilitacion
                    {
                        BeneficiarioId = benef.Id,
                        Sede = benef.SedeAsignada,
                        Etapa = etapa,
                        TallerMateria = taller,
                        FechaInicio = fecha,
                        Completado = (i % 3 == 0)
                    });
                    agregados++;
                }
                context.SaveChanges();
                totalAgregados += agregados;
                resumen.Add($"{agregados} Inscripciones de Rehabilitación");
            }

            // 8. VOLUNTARIOS (Garantizar mínimo 30)
            if (context.Voluntarios.Count() < 25)
            {
                var areas = new[]
                {
                    "Lectura Grabada de Libros y Materiales de Estudio",
                    "Acompañamiento en Trámites y Citas Médicas",
                    "Docencia y Apoyo en Talleres de Braille",
                    "Logística y Arbitraje en Torneos de Ajedrez Táctil",
                    "Operativos de Salud Visual y Prevención de Ceguera en Comunidades",
                    "Capacitación en Alfabetización Digital Inclusiva",
                    "Taller Protegido y Empaque de Productos Ecológicos"
                };

                var voluntariosNombres = new[]
                {
                    ("Lic. Patricia Alcántara", "001-8899001-2", "809-555-4011", "patricia.alcantara@correo.do"),
                    ("Ing. Gabriel Pichardo", "001-7788990-3", "829-555-4022", "gabriel.pichardo@correo.do"),
                    ("Dra. Nicole Fernández", "031-6677889-4", "849-555-4033", "nicole.fernandez@correo.do"),
                    ("Estudiante Luis Miguel Santana", "018-5566778-5", "809-555-4044", "luis.santana@correo.do"),
                    ("Prof. Mariana Estrella", "031-4455667-6", "829-555-4055", "mariana.estrella@correo.do"),
                    ("Lic. Alejandro Betances", "001-3344556-7", "849-555-4066", "alejandro.betances@correo.do"),
                    ("Sra. Dulce María Tavárez", "001-2233445-8", "809-555-4077", "dulce.tavarez@correo.do"),
                    ("Ing. Fernando Silvestre", "018-1122334-9", "829-555-4088", "fernando.silvestre@correo.do"),
                    ("Licda. Claudia Minaya", "001-9900112-0", "849-555-4099", "claudia.minaya@correo.do"),
                    ("Sr. Cristian Javier Morales", "031-8899002-1", "809-555-4100", "cristian.morales@correo.do")
                };

                var rand = new Random(65536);
                int target = 30 - context.Voluntarios.Count();
                int agregados = 0;

                for (int i = 0; i < target; i++)
                {
                    var baseVol = voluntariosNombres[i % voluntariosNombres.Length];
                    var area = areas[rand.Next(areas.Length)];

                    context.Voluntarios.Add(new Voluntario
                    {
                        NombreCompleto = $"{baseVol.Item1} ({i + 1})",
                        Cedula = baseVol.Item2,
                        Telefono = baseVol.Item3,
                        Correo = $"{i}_{baseVol.Item4}",
                        AreaApoyo = area,
                        FechaRegistro = DateTime.UtcNow.AddDays(-rand.Next(10, 300)),
                        Activo = true
                    });
                    agregados++;
                }
                context.SaveChanges();
                totalAgregados += agregados;
                resumen.Add($"{agregados} Voluntarios Solidarios");
            }

            // 9. HISTORIAL DE TURNOS Y TRANSACCIONES DE CAJA (Garantizar mínimo 50 transacciones)
            if (context.TransaccionesCaja.Count() < 45)
            {
                var rand = new Random(131072);
                int target = 50 - context.TransaccionesCaja.Count();
                int agregados = 0;

                var tipos = new[] { "VentaProducto", "MasajeTacto", "ConsultaOftalmologica", "AbonoPrestamo", "Donacion" };
                var metodos = new[] { "Efectivo", "Tarjeta", "Transferencia" };
                var cajeros = new[] { "Ana Rosario", "Carlos Méndez", "Patricia Solano" };

                for (int i = 0; i < target; i++)
                {
                    var tipo = tipos[rand.Next(tipos.Length)];
                    var metodo = metodos[rand.Next(metodos.Length)];
                    var cajero = cajeros[rand.Next(cajeros.Length)];
                    var fecha = DateTime.Now.AddDays(-rand.Next(1, 30)).AddHours(rand.Next(8, 17)).AddMinutes(rand.Next(0, 59));

                    decimal monto = tipo switch
                    {
                        "VentaProducto" => rand.Next(2, 15) * 150m,
                        "MasajeTacto" => (i % 2 == 0 ? 750m : 1000m),
                        "ConsultaOftalmologica" => 800m,
                        "AbonoPrestamo" => rand.Next(1, 5) * 1000m,
                        _ => rand.Next(1, 10) * 1000m
                    };

                    string ncf = (i % 3 == 0) ? $"B020000{(1000 + i):D4}" : $"B010000{(1000 + i):D4}";

                    context.TransaccionesCaja.Add(new TransaccionCaja
                    {
                        NumeroRecibo = $"REC-{fecha:yyyyMMdd}-{(1000 + i):D4}",
                        TipoTransaccion = tipo,
                        MontoTotal = monto,
                        MetodoPago = metodo,
                        FechaHora = fecha,
                        Cajero = cajero,
                        ClienteOBeneficiario = $"Cliente / Beneficiario #{i + 1}",
                        IdentificacionCliente = ncf,
                        Stan = $"STAN{(10000 + i):D6}",
                        EsAnulada = (i == 7 || i == 23), // 2 transacciones anuladas de ejemplo
                        MotivoAnulacion = (i == 7 || i == 23) ? "Error tipográfico en digitación de monto por el cajero" : null,
                        FechaHoraAnulacion = (i == 7 || i == 23) ? fecha.AddMinutes(10) : null,
                        AnuladoPor = (i == 7 || i == 23) ? "admin" : null
                    });
                    agregados++;
                }
                context.SaveChanges();
                totalAgregados += agregados;
                resumen.Add($"{agregados} Transacciones de Caja");
            }

            // 10. ÓRDENES DE PEDIDO DE CAJA (Para que la pantalla de Órdenes y el Dashboard tengan datos reales)
            if (context.OrdenesPedidoCaja.Count() < 10)
            {
                var ordenesEjemplo = new (string Cliente, string Rnc, string Creador, decimal Total, string Estado, string Items)[]
                {
                    ("Hospital Traumatológico Dr. Ney Arias Lora", "4-01-04567-8", "Laura Sánchez (Recepción)", 18500m, "Pendiente", "[{\"ProductoId\":1,\"Codigo\":\"BAST-01\",\"Nombre\":\"Bastón Blanco Plegable en Aluminio\",\"Cantidad\":20,\"PrecioUnitario\":650.00},{\"ProductoId\":30,\"Codigo\":\"ECO-01\",\"Nombre\":\"Suaper Institucional de Algodón Reciclado\",\"Cantidad\":22,\"PrecioUnitario\":250.00}]"),
                    ("Banco BHD - Sucursal Principal", "1-01-00123-4", "Laura Sánchez (Recepción)", 8400m, "Pendiente", "[{\"ProductoId\":31,\"Codigo\":\"ECO-02\",\"Nombre\":\"Trapeador Ecológico Extra-Absorbente\",\"Cantidad\":30,\"PrecioUnitario\":280.00}]"),
                    ("Colegio Calasanz Santo Domingo", "4-01-09876-1", "Laura Sánchez (Recepción)", 5200m, "Pendiente", "[{\"ProductoId\":10,\"Codigo\":\"BRAI-01\",\"Nombre\":\"Regleta Braille de Aluminio\",\"Cantidad\":8,\"PrecioUnitario\":450.00},{\"ProductoId\":12,\"Codigo\":\"BRAI-03\",\"Nombre\":\"Punzón Anatómico Ergonómico\",\"Cantidad\":10,\"PrecioUnitario\":160.00}]"),
                    ("Centro Médico UCE", "1-01-02345-6", "Laura Sánchez (Recepción)", 14300m, "Pendiente", "[{\"ProductoId\":32,\"Codigo\":\"ECO-03\",\"Nombre\":\"Desinfectante Multiuso Biodegradable (Galón)\",\"Cantidad\":25,\"PrecioUnitario\":150.00},{\"ProductoId\":1,\"Codigo\":\"BAST-01\",\"Nombre\":\"Bastón Blanco Plegable\",\"Cantidad\":16,\"PrecioUnitario\":650.00}]"),
                    ("Universidad Autónoma de Santo Domingo (UASD)", "4-01-00001-1", "Laura Sánchez (Recepción)", 9600m, "Pendiente", "[{\"ProductoId\":11,\"Codigo\":\"BRAI-02\",\"Nombre\":\"Regleta Braille de Bolsillo\",\"Cantidad\":20,\"PrecioUnitario\":250.00},{\"ProductoId\":14,\"Codigo\":\"BRAI-05\",\"Nombre\":\"Papel Ledger Especial para Braille (Resma)\",\"Cantidad\":12,\"PrecioUnitario\":380.00}]"),
                    ("Asociación Dominicana de Rehabilitación", "4-01-00555-2", "Laura Sánchez (Recepción)", 12800m, "Pendiente", "[{\"ProductoId\":1,\"Codigo\":\"BAST-01\",\"Nombre\":\"Bastón Blanco Plegable\",\"Cantidad\":15,\"PrecioUnitario\":650.00},{\"ProductoId\":2,\"Codigo\":\"BAST-02\",\"Nombre\":\"Bastón Verde Plegable (Baja Visión)\",\"Cantidad\":4,\"PrecioUnitario\":750.00}]"),
                    ("Ministerio de Salud Pública (MISPAS)", "4-01-00022-9", "Laura Sánchez (Recepción)", 24500m, "Pendiente", "[{\"ProductoId\":24,\"Codigo\":\"OPT-01\",\"Nombre\":\"Lupa Electrónica Digital Portátil HD\",\"Cantidad\":5,\"PrecioUnitario\":3900.00},{\"ProductoId\":1,\"Codigo\":\"BAST-01\",\"Nombre\":\"Bastón Blanco Plegable\",\"Cantidad\":8,\"PrecioUnitario\":650.00}]"),
                    ("Fundación Sur Futuro", "4-01-07890-3", "Laura Sánchez (Recepción)", 7500m, "Pendiente", "[{\"ProductoId\":30,\"Codigo\":\"ECO-01\",\"Nombre\":\"Suaper Institucional de Algodón Reciclado\",\"Cantidad\":25,\"PrecioUnitario\":260.00},{\"ProductoId\":34,\"Codigo\":\"ECO-05\",\"Nombre\":\"Cesta de Macramé en Fibras Naturales\",\"Cantidad\":2,\"PrecioUnitario\":500.00}]"),
                    ("Supermercados Nacional / CCN", "1-01-00777-3", "Laura Sánchez (Recepción)", 16800m, "Cobrada", "[{\"ProductoId\":30,\"Codigo\":\"ECO-01\",\"Nombre\":\"Suaper Institucional de Algodón Reciclado\",\"Cantidad\":60,\"PrecioUnitario\":280.00}]"),
                    ("Farmacia Carol - Sucursal 27 de Febrero", "1-01-01999-4", "Laura Sánchez (Recepción)", 6500m, "Cobrada", "[{\"ProductoId\":26,\"Codigo\":\"OPT-03\",\"Nombre\":\"Lupa Tipo Domo de Cristal de Lectura 4X\",\"Cantidad\":5,\"PrecioUnitario\":780.00},{\"ProductoId\":35,\"Codigo\":\"ECO-06\",\"Nombre\":\"Cojín Ergonómico de Aromaterapia\",\"Cantidad\":6,\"PrecioUnitario\":430.00}]"),
                    ("Grupo Puntacana", "1-01-03333-8", "Laura Sánchez (Recepción)", 22000m, "Cobrada", "[{\"ProductoId\":30,\"Codigo\":\"ECO-01\",\"Nombre\":\"Suaper Institucional de Algodón Reciclado\",\"Cantidad\":50,\"PrecioUnitario\":280.00},{\"ProductoId\":32,\"Codigo\":\"ECO-03\",\"Nombre\":\"Desinfectante Multiuso Biodegradable (Galón)\",\"Cantidad\":40,\"PrecioUnitario\":200.00}]")
                };

                int agregadosOrdenes = 0;
                for (int i = 0; i < ordenesEjemplo.Length; i++)
                {
                    var o = ordenesEjemplo[i];
                    context.OrdenesPedidoCaja.Add(new OrdenPedidoCaja
                    {
                        NumeroOrden = $"ORD-{DateTime.Now.AddHours(-i):yyyyMMdd}-{(100 + i):D3}",
                        FechaHora = DateTime.Now.AddHours(-i * 2).AddMinutes(-i * 5),
                        ClienteNombre = o.Cliente,
                        ClienteDocumento = o.Rnc,
                        CreadoPor = o.Creador,
                        Estado = o.Estado,
                        Total = o.Total,
                        ItemsJson = o.Items,
                        Notas = $"Pedido institucional para {o.Cliente}."
                    });
                    agregadosOrdenes++;
                }
                context.SaveChanges();
                totalAgregados += agregadosOrdenes;
                resumen.Add($"{agregadosOrdenes} Órdenes de Pedido");
            }

            string detalleTexto = string.Join(", ", resumen);
            return (totalAgregados, detalleTexto);
        }
    }
}
