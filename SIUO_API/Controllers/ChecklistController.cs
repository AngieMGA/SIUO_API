using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using SIUO_API.Services;

namespace SIUO_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChecklistController : ControllerBase
    {

        private readonly FtpService _ftpService;
        private readonly ChecklistRepository _checklistRepository;


        public ChecklistController(
            FtpService ftpService,
            ChecklistRepository checklistRepository)
        {
            _ftpService = ftpService;
            _checklistRepository = checklistRepository;
        }

        // GUARDAR CHECKLIST + JSON + EVIDENCIAS

        [HttpPost]
        public async Task<IActionResult> GuardarChecklist(
            [FromForm] string checklist,
            [FromForm] List<IFormFile>? evidencias)
        {
            Console.WriteLine("=================================");
            Console.WriteLine("CHECKLIST RECIBIDO");
            Console.WriteLine(
                "Datos del checklist recibidos correctamente."
            );

            if (string.IsNullOrWhiteSpace(checklist))
            {
                return BadRequest(new
                {
                    mensaje =
                        "No se recibieron los datos del checklist."
                });
            }

            // Leer el JSON recibido

            using var documento =
                JsonDocument.Parse(checklist);

            string? folio = null;

            if (documento.RootElement.TryGetProperty(
                "folio",
                out var folioElemento))
            {
                folio = folioElemento.GetString();
            }

            if (string.IsNullOrWhiteSpace(folio))
            {
                folio =
                    $"SIN-FOLIO-{DateTime.Now:yyyyMMddHHmmss}";
            }

            // Evitar caracteres/rutas no deseadas
            folio = Path.GetFileName(folio);

            Console.WriteLine($"Folio: {folio}");

// -----------------------------------------------------
// Obtener tipo de checklist
// -----------------------------------------------------

string? tipoChecklist = null;

if (documento.RootElement.TryGetProperty(
    "tipoChecklist",
    out var tipoChecklistElemento))
{
    tipoChecklist =
        tipoChecklistElemento.GetString();
}

// GUARDAR INSPECCIÓN PRINCIPAL EN SQL SERVER

int? idInspeccion = null;

if (!string.IsNullOrWhiteSpace(tipoChecklist))
{
    try
    {
        string? observacionesGenerales = null;
string? nombreRecibe = null;
string? nombreSupervisor = null;

// =========================================================
// CAMPOS GENERALES SG-F-24-01
// =========================================================

if (documento.RootElement.TryGetProperty(
    "observacionesSGF2401",
    out var observaciones2401Elemento)
    && observaciones2401Elemento.ValueKind == JsonValueKind.String)
{
    observacionesGenerales =
        observaciones2401Elemento.GetString();
}

// =========================================================
// CAMPOS GENERALES SG-F-24-33
// =========================================================

if (string.IsNullOrWhiteSpace(observacionesGenerales))
{
    if (documento.RootElement.TryGetProperty(
        "comentarios2433",
        out var comentariosElemento)
        && comentariosElemento.ValueKind == JsonValueKind.String)
    {
        observacionesGenerales =
            comentariosElemento.GetString();
    }
}

// =========================================================
// NOMBRE DE QUIEN RECIBE
// =========================================================

if (documento.RootElement.TryGetProperty(
    "nombreRecibe",
    out var recibe2401Elemento)
    && recibe2401Elemento.ValueKind == JsonValueKind.String)
{
    nombreRecibe =
        recibe2401Elemento.GetString();
}

if (string.IsNullOrWhiteSpace(nombreRecibe))
{
    if (documento.RootElement.TryGetProperty(
        "nombreRecibe2433",
        out var recibe2433Elemento)
        && recibe2433Elemento.ValueKind == JsonValueKind.String)
    {
        nombreRecibe =
            recibe2433Elemento.GetString();
    }
}

// =========================================================
// NOMBRE DEL SUPERVISOR
// =========================================================

if (documento.RootElement.TryGetProperty(
    "nombreSupervisor",
    out var supervisor2401Elemento)
    && supervisor2401Elemento.ValueKind == JsonValueKind.String)
{
    nombreSupervisor =
        supervisor2401Elemento.GetString();
}

if (string.IsNullOrWhiteSpace(nombreSupervisor))
{
    if (documento.RootElement.TryGetProperty(
        "nombreSupervisor2433",
        out var supervisor2433Elemento)
        && supervisor2433Elemento.ValueKind == JsonValueKind.String)
    {
        nombreSupervisor =
            supervisor2433Elemento.GetString();
    }
}

        DateTime fechaInspeccion = DateTime.Now.Date;
        TimeSpan horaInspeccion = DateTime.Now.TimeOfDay;

        if (documento.RootElement.TryGetProperty(
            "fecha",
            out var fechaElemento) &&
            fechaElemento.ValueKind == JsonValueKind.String &&
            DateTime.TryParse(
                fechaElemento.GetString(),
                out var fechaParseada))
        {
            fechaInspeccion = fechaParseada.Date;
        }

        string? status = null;

if (
    tipoChecklist.Equals(
        "CHK-TRANSPORTE",
        StringComparison.OrdinalIgnoreCase
    )
    ||
    tipoChecklist.Equals(
        "RH-F-01-21",
        StringComparison.OrdinalIgnoreCase
    )
)
{
    status = "PENDIENTE";
}

string? identificadorDispositivo = null;

if (documento.RootElement.TryGetProperty(
    "identificadorDispositivo",
    out var dispositivoElemento)
    && dispositivoElemento.ValueKind == JsonValueKind.String)
{
    identificadorDispositivo =
        dispositivoElemento.GetString();
}

        idInspeccion =
            await _checklistRepository.GuardarInspeccionAsync(
                tipoChecklist,
                folio,
                fechaInspeccion,
                horaInspeccion,
                status,
                observacionesGenerales,
                nombreRecibe,
                nombreSupervisor,
                identificadorDispositivo
            );

        Console.WriteLine(
            $"Inspección guardada en SQL. ID: {idInspeccion}"
        );
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"Error al guardar inspección en SQL: {ex.Message}"
        );

        return StatusCode(500, new
        {
            mensaje = "El JSON se recibió, pero no se pudo guardar la inspección en SQL Server.",
            error = ex.Message
        });
    }
}

// -----------------------------------------------------
// Obtener área de materia prima
// -----------------------------------------------------

string? areaMateriaPrima = null;

if (documento.RootElement.TryGetProperty(
    "areaMateriaPrima",
    out var areaElemento))
{
    areaMateriaPrima =
        areaElemento.GetString();
}


// GUARDAR DATOS DE RECEPCIÓN
// CHECKLIST: SG-F-24-01

if (
    tipoChecklist?.Trim().Equals(
        "SG-F-24-01",
        StringComparison.OrdinalIgnoreCase
    ) == true
    && idInspeccion.HasValue
)
{
    string? ObtenerTextoRecepcion(string nombrePropiedad)
    {
        if (
            documento.RootElement.TryGetProperty(
                nombrePropiedad,
                out var elemento
            )
            && elemento.ValueKind == JsonValueKind.String
        )
        {
            return elemento.GetString();
        }

        return null;
    }

    string? proveedor =
        ObtenerTextoRecepcion("proveedor");

    string? material =
        ObtenerTextoRecepcion("material");

    string? operador =
        ObtenerTextoRecepcion("operador");

    string? lote =
        ObtenerTextoRecepcion("lote");

    string? turno =
        ObtenerTextoRecepcion("turno");

    string? diseno =
        ObtenerTextoRecepcion("diseno");

    string? especificarMaterial =
        ObtenerTextoRecepcion("materialEspecificado");

    string? tripulacion =
        ObtenerTextoRecepcion("tripulacion");

    string? placasNumero =
        ObtenerTextoRecepcion("placasNumero");

    string? ordenCompra =
        ObtenerTextoRecepcion("ordenCompra");

    string? facturaRemision =
        ObtenerTextoRecepcion("facturaRemision");

    string? alergenoTexto =
        ObtenerTextoRecepcion("alergeno");

    bool? alergenoMicroSensitivo = null;

    if (
        !string.IsNullOrWhiteSpace(
            alergenoTexto
        )
    )
    {
        if (
            alergenoTexto.Equals(
                "SI",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            alergenoMicroSensitivo = true;
        }
        else if (
            alergenoTexto.Equals(
                "NO",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            alergenoMicroSensitivo = false;
        }
    }

    await _checklistRepository.GuardarDatosRecepcionAsync(
        idInspeccion.Value,
        areaMateriaPrima ?? "",
        material,
        proveedor ?? "",
        operador ?? "",
        lote,
        turno,
        diseno,
        especificarMaterial,
        tripulacion,
        placasNumero,
        ordenCompra,
        facturaRemision,
        alergenoMicroSensitivo
    );

    Console.WriteLine(
        "Datos de recepción de SG-F-24-01 guardados en SQL."
    );
}

// GUARDAR RESPUESTAS SG-F-24-01
// ÁREA: Lata Vacía
// =========================================================

if (
    tipoChecklist?.Trim().Equals(
        "SG-F-24-01",
        StringComparison.OrdinalIgnoreCase
    ) == true
    && idInspeccion.HasValue
    && areaMateriaPrima?.Trim().Equals(
        "Lata Vacía",
        StringComparison.OrdinalIgnoreCase
    ) == true
)
{
    string? NormalizarRespuestaSGF2401(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        return valor.Trim().ToLowerInvariant() switch
        {
            "cumple" => "CUMPLE",
            "nocumple" => "NO_CUMPLE",
            "no_cumple" => "NO_CUMPLE",
            "na" => "NA",
            _ => null
        };
    }

    var preguntasLataVacia = new[]
    {
        // =============================================
        // CONDICIONES DEL TRANSPORTE
        // =============================================
        "TR-001",
        "TR-002",
        "TR-003",
        "TR-004",
        "TR-005",
        "TR-006",
        "TR-008",
        "TR-011",

        // =============================================
        // CONDICIONES DEL MATERIAL
        // =============================================
        "MAT-002",
        "MAT-003",
        "MAT-004",
        "MAT-005",
        "MAT-006",
        "MAT-009",
        "MAT-010",
        "MAT-012",
        "MAT-013",

        // =============================================
        // CALIDAD DEL SERVICIO
        // =============================================
        "SER-001",
        "SER-002",
        "SER-003",
        "SER-004"
    };

    foreach (string codigoPregunta in preguntasLataVacia)
    {
        if (
            documento.RootElement.TryGetProperty(
                codigoPregunta,
                out var elementoRespuesta
            )
            && elementoRespuesta.ValueKind ==
                JsonValueKind.String
        )
        {
            string? valorReact =
                elementoRespuesta.GetString();

            string? valorSQL =
                NormalizarRespuestaSGF2401(valorReact);

            if (!string.IsNullOrWhiteSpace(valorSQL))
            {
                await _checklistRepository
                    .GuardarRespuestaOpcionPorChecklistAsync(
                        idInspeccion.Value,
                        "SG-F-24-01",
                        codigoPregunta,
                        valorSQL,
                        null
                    );

                Console.WriteLine(
                    $"SG-F-24-01 | {codigoPregunta} = {valorSQL}"
                );
            }
        }
    }

    Console.WriteLine(
        "Respuestas SG-F-24-01 de Lata Vacía guardadas en SQL."
    );
}

// =========================================================
// GUARDAR RESPUESTAS SG-F-24-01
// ÁREA: Materias Primas
// =========================================================

if (
    tipoChecklist?.Trim().Equals(
        "SG-F-24-01",
        StringComparison.OrdinalIgnoreCase
    ) == true
    && idInspeccion.HasValue
    && areaMateriaPrima?.Trim().Equals(
        "Materias Primas",
        StringComparison.OrdinalIgnoreCase
    ) == true
)
{
    // ---------------------------------------------------------
    // Obtener material
    // ---------------------------------------------------------

    string? material = null;

    if (
        documento.RootElement.TryGetProperty(
            "material",
            out var materialElemento
        )
        && materialElemento.ValueKind == JsonValueKind.String
    )
    {
        material = materialElemento.GetString();
    }

    // ---------------------------------------------------------
    // Normalizar respuesta
    // ---------------------------------------------------------

    string? NormalizarRespuestaSGF2401(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        return valor.Trim().ToLowerInvariant() switch
        {
            "cumple" => "CUMPLE",
            "nocumple" => "NO_CUMPLE",
            "no_cumple" => "NO_CUMPLE",
            "na" => "NA",
            _ => null
        };
    }

    // ---------------------------------------------------------
    // Material seleccionado
    // ---------------------------------------------------------

    var preguntasMateriasPrimas = new List<string>();

    bool esAzucar =
        material?.Trim().Equals(
            "Azúcar",
            StringComparison.OrdinalIgnoreCase
        ) == true;

    bool esFructosa =
        material?.Trim().Equals(
            "Fructosa 55",
            StringComparison.OrdinalIgnoreCase
        ) == true;

    bool esConcentrado =
        material?.Trim().Equals(
            "Concentrado",
            StringComparison.OrdinalIgnoreCase
        ) == true;

    bool esOtro =
        material?.Trim().Equals(
            "Otro",
            StringComparison.OrdinalIgnoreCase
        ) == true;

    // =========================================================
    // AZÚCAR
    // =========================================================

    if (esAzucar)
    {
        preguntasMateriasPrimas.AddRange(
            new[]
            {
                // TRANSPORTE
                "TR-001",
                "TR-002",
                "TR-003",
                "TR-004",
                "TR-005",
                "TR-006",
                "TR-008",
                "TR-010",
                "TR-011",

                // MATERIAL
                "MAT-001",
                "MAT-002",
                "MAT-003",
                "MAT-004",
                "MAT-005",
                "MAT-006",
                "MAT-007",
                "MAT-008",
                "MAT-009",
                "MAT-010",
                "MAT-012",
                "MAT-013",

                // SERVICIO
                "SER-001",
                "SER-002",
                "SER-003",
                "SER-004",

                // SACO
                "SAC-001",
                "SAC-002",
                "SAC-003",
                "SAC-004",
                "SAC-005",
                "SAC-006",
                "SAC-007"
            }
        );
    }

    // =========================================================
    // FRUCTOSA 55
    // =========================================================

    else if (esFructosa)
    {
        preguntasMateriasPrimas.AddRange(
            new[]
            {
                // TRANSPORTE
                "TR-001",
                "TR-002",
                "TR-003",
                "TR-004",
                "TR-005",
                "TR-006",
                "TR-008",
                "TR-010",
                "TR-011",

                // MATERIAL
                "MAT-001",
                "MAT-002",
                "MAT-003",
                "MAT-004",
                "MAT-005",
                "MAT-006",
                "MAT-008",
                "MAT-009",
                "MAT-010",
                "MAT-011",
                "MAT-012",
                "MAT-013",

                // SERVICIO
                "SER-001",
                "SER-002",
                "SER-003",
                "SER-004"
            }
        );
    }

    // =========================================================
    // CONCENTRADO
    // =========================================================

    else if (esConcentrado)
    {
        preguntasMateriasPrimas.AddRange(
            new[]
            {
                // TRANSPORTE
                "TR-001",
                "TR-002",
                "TR-003",
                "TR-004",
                "TR-005",
                "TR-006",
                "TR-008",
                "TR-009",
                "TR-010",
                "TR-011",

                // MATERIAL
                "MAT-002",
                "MAT-003",
                "MAT-004",
                "MAT-005",
                "MAT-006",
                "MAT-008",
                "MAT-009",
                "MAT-010",
                "MAT-011",
                "MAT-012",
                "MAT-013",

                // SERVICIO
                "SER-001",
                "SER-002",
                "SER-003",
                "SER-004"
            }
        );
    }

    // =========================================================
    // OTRO
    // =========================================================

    else if (esOtro)
    {
        preguntasMateriasPrimas.AddRange(
            new[]
            {
                // TRANSPORTE
                "TR-001",
                "TR-002",
                "TR-003",
                "TR-004",
                "TR-005",
                "TR-006",
                "TR-007",
                "TR-008",
                "TR-009",
                "TR-010",
                "TR-011",

                // MATERIAL
                "MAT-001",
                "MAT-002",
                "MAT-003",
                "MAT-004",
                "MAT-005",
                "MAT-006",
                "MAT-007",
                "MAT-008",
                "MAT-009",
                "MAT-010",
                "MAT-011",
                "MAT-012",
                "MAT-013",

                // TARIMAS
                "TAR-001",
                "TAR-002",
                "TAR-003",
                "TAR-004",

                // SERVICIO
                "SER-001",
                "SER-002",
                "SER-003",
                "SER-004",

                // SACO
                "SAC-001",
                "SAC-002",
                "SAC-003",
                "SAC-004",
                "SAC-005",
                "SAC-006",
                "SAC-007"
            }
        );
    }

    // ---------------------------------------------------------
    // Validar material
    // ---------------------------------------------------------

    if (preguntasMateriasPrimas.Count == 0)
    {
        Console.WriteLine(
            $"Material de Materias Primas no reconocido: [{material}]"
        );
    }
    else
    {
        // -----------------------------------------------------
        // Guardar respuestas
        // -----------------------------------------------------

        foreach (string codigoPregunta in preguntasMateriasPrimas)
        {
            if (
                documento.RootElement.TryGetProperty(
                    codigoPregunta,
                    out var elementoRespuesta
                )
                && elementoRespuesta.ValueKind ==
                    JsonValueKind.String
            )
            {
                string? valorReact =
                    elementoRespuesta.GetString();

                string? valorSQL =
                    NormalizarRespuestaSGF2401(
                        valorReact
                    );

                if (!string.IsNullOrWhiteSpace(valorSQL))
                {
                    string? observaciones = null;

                    // -------------------------------------------------
                    // Temperatura de Concentrado - TR-009
                    // -------------------------------------------------

                    if (
                        codigoPregunta == "TR-009"
                        && esConcentrado
                        && documento.RootElement.TryGetProperty(
                            "temperaturaConcentrado",
                            out var temperaturaElemento
                        )
                        && temperaturaElemento.ValueKind ==
                            JsonValueKind.String
                    )
                    {
                        string? temperatura =
                            temperaturaElemento.GetString();

                        if (!string.IsNullOrWhiteSpace(temperatura))
                        {
                            observaciones =
                                $"Temperatura: {temperatura} °C";
                        }
                    }

                    // -------------------------------------------------
                    // Número de sello - TR-011
                    // -------------------------------------------------

                    if (
                        codigoPregunta == "TR-011"
                        && documento.RootElement.TryGetProperty(
                            "numeroSello",
                            out var selloElemento
                        )
                        && selloElemento.ValueKind ==
                            JsonValueKind.String
                    )
                    {
                        string? numeroSello =
                            selloElemento.GetString();

                        if (!string.IsNullOrWhiteSpace(numeroSello))
                        {
                            observaciones =
                                $"Número de sello: {numeroSello}";
                        }
                    }

                    await _checklistRepository
                        .GuardarRespuestaOpcionPorChecklistAsync(
                            idInspeccion.Value,
                            "SG-F-24-01",
                            codigoPregunta,
                            valorSQL,
                            observaciones
                        );

                    Console.WriteLine(
                        $"SG-F-24-01 | Materias Primas | {material} | " +
                        $"{codigoPregunta} = {valorSQL}" +
                        (
                            string.IsNullOrWhiteSpace(observaciones)
                                ? ""
                                : $" | {observaciones}"
                        )
                    );
                }
            }
        }

        Console.WriteLine(
            $"Respuestas SG-F-24-01 de Materias Primas ({material}) guardadas en SQL."
        );
    }
}

// =========================================================
// GUARDAR DATOS ESPECIALES SG-F-24-01
// PESAJE + MERMA + SUPERSACO
// =========================================================

if (
    tipoChecklist?.Trim().Equals(
        "SG-F-24-01",
        StringComparison.OrdinalIgnoreCase
    ) == true
    && idInspeccion.HasValue
    && areaMateriaPrima?.Trim().Equals(
        "Materias Primas",
        StringComparison.OrdinalIgnoreCase
    ) == true
)
{
    // ---------------------------------------------------------
    // Obtener material
    // ---------------------------------------------------------

    string? material = null;

    if (
        documento.RootElement.TryGetProperty(
            "material",
            out var materialElemento
        )
        && materialElemento.ValueKind == JsonValueKind.String
    )
    {
        material = materialElemento.GetString();
    }

    bool esAzucar =
        material?.Trim().Equals(
            "Azúcar",
            StringComparison.OrdinalIgnoreCase
        ) == true;

    bool esConcentrado =
        material?.Trim().Equals(
            "Concentrado",
            StringComparison.OrdinalIgnoreCase
        ) == true;

    bool esOtro =
        material?.Trim().Equals(
            "Otro",
            StringComparison.OrdinalIgnoreCase
        ) == true;

    // ---------------------------------------------------------
    // Función para obtener texto
    // ---------------------------------------------------------

    string? ObtenerTextoEspecial(string nombrePropiedad)
    {
        if (
            documento.RootElement.TryGetProperty(
                nombrePropiedad,
                out var elemento
            )
            && elemento.ValueKind == JsonValueKind.String
        )
        {
            return elemento.GetString();
        }

        return null;
    }

    // ---------------------------------------------------------
    // Función para obtener decimal
    // ---------------------------------------------------------

    decimal? ObtenerDecimalEspecial(string nombrePropiedad)
    {
        string? texto =
            ObtenerTextoEspecial(nombrePropiedad);

        if (
            string.IsNullOrWhiteSpace(texto)
        )
        {
            return null;
        }

        if (
            decimal.TryParse(
                texto,
                out decimal valor
            )
        )
        {
            return valor;
        }

        Console.WriteLine(
            $"No se pudo convertir a decimal: {nombrePropiedad} = [{texto}]"
        );

        return null;
    }

    // =========================================================
    // PESAJE
    // Se utiliza para Azúcar y Otro
    // =========================================================

    if (
        esAzucar
        || esOtro
    )
    {
        decimal? pesoInicial =
            ObtenerDecimalEspecial("pesoInicial");

        decimal? pesoFinal =
            ObtenerDecimalEspecial("pesoFinal");

        decimal? tara =
            ObtenerDecimalEspecial("tara");

        string? numeroCodigo =
            ObtenerTextoEspecial("numeroCodigo");

        decimal? tq1Inicial =
            ObtenerDecimalEspecial("tq1Inicial");

        decimal? tq1Final =
            ObtenerDecimalEspecial("tq1Final");

        decimal? psiTq1Inicial =
            ObtenerDecimalEspecial("psiTq1Inicial");

        decimal? psiTq1Final =
            ObtenerDecimalEspecial("psiTq1Final");

        decimal? tq2Inicial =
            ObtenerDecimalEspecial("tq2Inicial");

        decimal? tq2Final =
            ObtenerDecimalEspecial("tq2Final");

        decimal? psiTq2Inicial =
            ObtenerDecimalEspecial("psiTq2Inicial");

        decimal? psiTq2Final =
            ObtenerDecimalEspecial("psiTq2Final");

        // Guardar solamente si existe algún dato de pesaje
        if (
            pesoInicial.HasValue
            || pesoFinal.HasValue
            || tara.HasValue
            || !string.IsNullOrWhiteSpace(numeroCodigo)
            || tq1Inicial.HasValue
            || tq1Final.HasValue
            || psiTq1Inicial.HasValue
            || psiTq1Final.HasValue
            || tq2Inicial.HasValue
            || tq2Final.HasValue
            || psiTq2Inicial.HasValue
            || psiTq2Final.HasValue
        )
        {
            await _checklistRepository
                .GuardarPesajeSGF2401Async(
                    idInspeccion.Value,
                    pesoInicial,
                    pesoFinal,
                    tara,
                    numeroCodigo,
                    tq1Inicial,
                    tq1Final,
                    psiTq1Inicial,
                    psiTq1Final,
                    tq2Inicial,
                    tq2Final,
                    psiTq2Inicial,
                    psiTq2Final
                );

            Console.WriteLine(
                $"Pesaje SG-F-24-01 guardado. ID inspección: {idInspeccion.Value}"
            );
        }
    }

    // =========================================================
    // MERMA
    // Azúcar + Concentrado + Otro
    // =========================================================

    if (
        esAzucar
        || esConcentrado
        || esOtro
    )
    {
        string[] camposSacos =
        {
            "saco1Kg",
            "saco2Kg",
            "saco3Kg",
            "saco4Kg",
            "saco5Kg",
            "saco6Kg",
            "saco7Kg",
            "saco8Kg"
        };

        for (
            int i = 0;
            i < camposSacos.Length;
            i++
        )
        {
            decimal? pesoSaco =
                ObtenerDecimalEspecial(
                    camposSacos[i]
                );

            if (pesoSaco.HasValue)
            {
                await _checklistRepository
                    .GuardarMermaSGF2401Async(
                        idInspeccion.Value,
                        i + 1,
                        pesoSaco.Value
                    );

                Console.WriteLine(
                    $"Merma SG-F-24-01 | Saco {i + 1} = {pesoSaco.Value}"
                );
            }
        }

        // -----------------------------------------------------
        // Resumen de merma
        // -----------------------------------------------------

        decimal? totalKg =
            ObtenerDecimalEspecial("totalKg");

        decimal? promedioKg =
            ObtenerDecimalEspecial("promedioKg");

        decimal? diferenciaKg =
            ObtenerDecimalEspecial("diferenciaKg");

        decimal? mermaKg =
            ObtenerDecimalEspecial("mermaKg");

        if (
            totalKg.HasValue
            || promedioKg.HasValue
            || diferenciaKg.HasValue
            || mermaKg.HasValue
        )
        {
            await _checklistRepository
                .GuardarResumenMermaSGF2401Async(
                    idInspeccion.Value,
                    totalKg,
                    promedioKg,
                    diferenciaKg,
                    mermaKg
                );

            Console.WriteLine(
                $"Resumen de merma SG-F-24-01 guardado. ID inspección: {idInspeccion.Value}"
            );
        }
    }

    // =========================================================
    // ESTADO DE SUPERSACOS
    // Azúcar + Concentrado + Otro
    // =========================================================

    if (
        esAzucar
        || esConcentrado
        || esOtro
    )
    {
        string[] camposSupersaco =
        {
            "supersaco1",
            "supersaco2",
            "supersaco3",
            "supersaco4",
            "supersaco5",
            "supersaco6",
            "supersaco7",
            "supersaco8",
            "supersaco9"
        };

        for (
            int i = 0;
            i < camposSupersaco.Length;
            i++
        )
        {
            string? estado =
                ObtenerTextoEspecial(
                    camposSupersaco[i]
                );

            if (!string.IsNullOrWhiteSpace(estado))
            {
                await _checklistRepository
                    .GuardarEstadoSupersacoSGF2401Async(
                        idInspeccion.Value,
                        "ESTADO_SUPERSACO",
                        i + 1,
                        estado
                    );

                Console.WriteLine(
                    $"Supersaco {i + 1} = {estado}"
                );
            }
        }
    }
}

// =========================================================
// GUARDAR RH-F-01-21
// DATOS GENERALES + OPERADOR + TRANSPORTE + RESPUESTAS
// =========================================================

if (
    tipoChecklist?.Trim().Equals(
        "RH-F-01-21",
        StringComparison.OrdinalIgnoreCase
    ) == true
    && idInspeccion.HasValue
)
{
    // -----------------------------------------------------
    // FUNCIÓN PARA OBTENER TEXTO
    // -----------------------------------------------------

    string? ObtenerTextoRH(string nombrePropiedad)
    {
        if (
            documento.RootElement.TryGetProperty(
                nombrePropiedad,
                out var elemento
            )
            && elemento.ValueKind == JsonValueKind.String
        )
        {
            return elemento.GetString();
        }

        return null;
    }

    // =====================================================
    // DATOS DEL OPERADOR
    // =====================================================

    string? nombreOperadorRH =
        ObtenerTextoRH("nombreOperadorRHF");

    string? telefonoOperador =
        ObtenerTextoRH("telefonoOperador");

    string? presentacionOperador =
        ObtenerTextoRH("presentacionOperador");

    string? numeroLicencia =
        ObtenerTextoRH("numeroLicencia");

    string? numeroTarjeta =
        ObtenerTextoRH("numeroTarjeta");

    string? numeroIMSS =
        ObtenerTextoRH("numeroIMSS");

    string? numeroSeguro =
        ObtenerTextoRH("numeroSeguro");

    string? numeroCartaPorte =
        ObtenerTextoRH("numeroCartaPorte");

    string? observacionesEPP =
        ObtenerTextoRH("observacionesEPP");

    if (string.IsNullOrWhiteSpace(nombreOperadorRH))
    {
        throw new InvalidOperationException(
            "RH-F-01-21 requiere el nombre del operador."
        );
    }

    int idOperadorRH =
        await _checklistRepository.ObtenerOCrearOperadorAsync(
            nombreOperadorRH
        );

    await _checklistRepository.GuardarDatosOperadorRHF0121Async(
        idInspeccion.Value,
        idOperadorRH,
        presentacionOperador,
        observacionesEPP,
        telefonoOperador,
        numeroLicencia,
        numeroTarjeta,
        numeroIMSS,
        numeroSeguro,
        numeroCartaPorte
    );

    await _checklistRepository.RelacionarOperadorConInspeccionAsync(
        idInspeccion.Value,
        idOperadorRH
    );

    Console.WriteLine(
        $"RH-F-01-21 | Operador guardado. ID operador: {idOperadorRH}"
    );

    // =====================================================
    // DATOS GENERALES DEL TRANSPORTE
    // =====================================================

    string? lineaTransporte =
        ObtenerTextoRH("lineaTransporteRHF");

    string? numeroTractor =
        ObtenerTextoRH("numeroTractor");

    string? numeroRemolque1 =
        ObtenerTextoRH("numeroRemolque1");

    string? numeroRemolque2 =
        ObtenerTextoRH("numeroRemolque2");

    string? placasTractor =
        ObtenerTextoRH("placasTractor");

    string? placasRemolque1 =
        ObtenerTextoRH("placasRemolque1");

    string? placasRemolque2 =
        ObtenerTextoRH("placasRemolque2");

    if (string.IsNullOrWhiteSpace(lineaTransporte))
    {
        throw new InvalidOperationException(
            "RH-F-01-21 requiere la línea de transporte."
        );
    }

    int idTransporteRH =
        await _checklistRepository.ObtenerOCrearTransporteRHF0121Async(
            lineaTransporte,
            numeroTractor,
            numeroRemolque1,
            numeroRemolque2,
            placasTractor,
            placasRemolque1,
            placasRemolque2
        );

    Console.WriteLine(
        $"RH-F-01-21 | Transporte guardado. ID transporte: {idTransporteRH}"
    );

    // =====================================================
    // DATOS ESPECÍFICOS DE RH
    // =====================================================

    string? origenDescarga =
        ObtenerTextoRH("origenDescarga");

    string? destinoCarga =
        ObtenerTextoRH("destinoCarga");

    string? numeroSellos =
        ObtenerTextoRH("numeroSellos");

    string? numeroDelivery =
        ObtenerTextoRH("numeroDelivery");

    string? tipoTransporte =
        ObtenerTextoRH("tipoTransporte");

    // En RH, "Configuración" corresponde a tipo_unidad
    string? tipoUnidad =
        ObtenerTextoRH("configuracion");

    string? nivelTanque1 =
        ObtenerTextoRH("tanque1");

    string? nivelTanque2 =
        ObtenerTextoRH("tanque2");

    string? refrigerado =
        ObtenerTextoRH("refrigerado");

    string? temperaturaTexto =
        ObtenerTextoRH("temperatura");

    decimal? temperatura = null;

    if (
        !string.IsNullOrWhiteSpace(temperaturaTexto)
        && decimal.TryParse(
            temperaturaTexto,
            out decimal temperaturaParseada
        )
    )
    {
        temperatura = temperaturaParseada;
    }

    // =====================================================
    // FECHAS / HORAS
    // =====================================================

    DateTime? fechaHoraLlegada = null;

    string? fechaHoraLlegadaTexto =
        ObtenerTextoRH("fechaHoraLlegada");

    if (
        !string.IsNullOrWhiteSpace(fechaHoraLlegadaTexto)
        && DateTime.TryParse(
            fechaHoraLlegadaTexto,
            out DateTime llegadaParseada
        )
    )
    {
        fechaHoraLlegada = llegadaParseada;
    }

    DateTime? fechaHoraSalida = null;

    string? fechaHoraSalidaTexto =
        ObtenerTextoRH("fechaHoraSalida");

    if (
        !string.IsNullOrWhiteSpace(fechaHoraSalidaTexto)
        && DateTime.TryParse(
            fechaHoraSalidaTexto,
            out DateTime salidaParseada
        )
    )
    {
        fechaHoraSalida = salidaParseada;
    }

    // =====================================================
    // RESULTADO
    // =====================================================

    string? resultadoFinal =
        ObtenerTextoRH("resultadoFinal");

    string? rampaAsignada =
        ObtenerTextoRH("rampaAsignada");

    string? comentarios =
        ObtenerTextoRH("comentarios");

    // =====================================================
    // GUARDAR DATOS_TRANSPORTE_RH
    // =====================================================

    await _checklistRepository.GuardarDatosTransporteRHF0121Async(
        idInspeccion.Value,
        idOperadorRH,
        idTransporteRH,
        origenDescarga,
        destinoCarga,
        numeroSellos,
        numeroDelivery,
        tipoTransporte,
        tipoUnidad,
        nivelTanque1,
        nivelTanque2,
        refrigerado,
        fechaHoraLlegada,
        fechaHoraSalida,
        temperatura,
        resultadoFinal,
        rampaAsignada,
        comentarios
    );

    Console.WriteLine(
        "RH-F-01-21 | Datos específicos de transporte guardados."
    );

    // =====================================================
    // RESPUESTAS DEL CHECKLIST
    // 8 OP + 4 TR + 22 INSP = 34
    // =====================================================

    string? NormalizarRespuestaRH(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        return valor.Trim().ToLowerInvariant() switch
        {
            "cumple" => "CUMPLE",
            "nocumple" => "NO_CUMPLE",
            "no_cumple" => "NO_CUMPLE",
            _ => null
        };
    }

    var preguntasRH = new[]
    {
        // DATOS DEL OPERADOR
        "OP-001",
        "OP-002",
        "OP-003",
        "OP-004",
        "OP-005",
        "OP-006",
        "OP-007",
        "OP-008",

        // DATOS DEL TRANSPORTE
        "TR-001",
        "TR-002",
        "TR-003",
        "TR-004",

        // INSPECCIÓN
        "INSP-001",
        "INSP-002",
        "INSP-003",
        "INSP-004",
        "INSP-005",
        "INSP-006",
        "INSP-007",
        "INSP-008",
        "INSP-009",
        "INSP-010",
        "INSP-011",
        "INSP-012",
        "INSP-013",
        "INSP-014",
        "INSP-015",
        "INSP-016",
        "INSP-017",
        "INSP-018",
        "INSP-019",
        "INSP-020",
        "INSP-021",
        "INSP-022"
    };

    foreach (string codigoPregunta in preguntasRH)
    {
        if (
            documento.RootElement.TryGetProperty(
                codigoPregunta,
                out var elementoRespuesta
            )
            && elementoRespuesta.ValueKind ==
                JsonValueKind.String
        )
        {
            string? valorReact =
                elementoRespuesta.GetString();

            string? valorSQL =
                NormalizarRespuestaRH(valorReact);

            if (!string.IsNullOrWhiteSpace(valorSQL))
            {
                await _checklistRepository
                    .GuardarRespuestaOpcionPorChecklistAsync(
                        idInspeccion.Value,
                        "RH-F-01-21",
                        codigoPregunta,
                        valorSQL,
                        null
                    );

                Console.WriteLine(
                    $"RH-F-01-21 | {codigoPregunta} = {valorSQL}"
                );
            }
        }
    }

    Console.WriteLine(
        "Respuestas RH-F-01-21 guardadas en SQL."
    );
}

// Determinar carpeta según el TIPO DE CHECKLIST

string carpetaChecklist;

if (!string.IsNullOrWhiteSpace(tipoChecklist))
{
    carpetaChecklist = tipoChecklist;
}
else
{
    carpetaChecklist = "Otros";
}

// -----------------------------------------------------
// Obtener número de DELIVERY
// -----------------------------------------------------

string? delivery = null;

if (documento.RootElement.TryGetProperty(
    "delivery",
    out var deliveryElemento))
{
    delivery = deliveryElemento.GetString();
}

if (string.IsNullOrWhiteSpace(delivery))
{
    delivery = "SIN-DELIVERY";
}

delivery = Path.GetFileName(delivery);
// =========================================================
// GUARDAR DATOS GENERALES DE SG-F-24-33 EN SQL SERVER

Console.WriteLine("========================================");
Console.WriteLine("PRUEBA BLOQUE TRANSPORTE");
Console.WriteLine($"tipoChecklist = [{tipoChecklist}]");
Console.WriteLine($"idInspeccion = [{idInspeccion}]");
Console.WriteLine("========================================");

if (
    tipoChecklist?.Trim().Equals(
        "SG-F-24-33",
        StringComparison.OrdinalIgnoreCase
    ) == true
    && idInspeccion.HasValue
)
{

    Console.WriteLine(
    $"*** BLOQUE DE LLANTAS EJECUTADO *** ID INSPECCION: {idInspeccion.Value}"
);

Console.WriteLine(
    $"*** TIPO CHECKLIST: [{tipoChecklist}] ***"
);

    DateTime? fechaRecepcion = null;
    DateTime? fechaTermino = null;

    string? producto = null;
    string? placas = null;
    string? horaRecepcion = null;
    string? horaTermino = null;
    string? numeroSellos = null;
    string? numeroFactura = null;
    string? turno = null;
    string? tripulacion = null;

    // Función para obtener únicamente valores de texto
    string? ObtenerTexto(string nombrePropiedad)
    {
        if (
            documento.RootElement.TryGetProperty(
                nombrePropiedad,
                out var elemento
            )
            && elemento.ValueKind == JsonValueKind.String
        )
        {
            return elemento.GetString();
        }

        return null;
    }

    // Producto
    producto = ObtenerTexto("nombreProducto");

    // Fecha de recepción
    string? fechaRecepcionTexto =
        ObtenerTexto("fechaRecepcion");

    if (
        !string.IsNullOrWhiteSpace(fechaRecepcionTexto)
        && DateTime.TryParse(
            fechaRecepcionTexto,
            out var fechaRecepcionParseada
        )
    )
    {
        fechaRecepcion = fechaRecepcionParseada.Date;
    }

    // Fecha de término de recepción
    string? fechaTerminoTexto =
        ObtenerTexto("fechaTerminoRecepcion");

    if (
        !string.IsNullOrWhiteSpace(fechaTerminoTexto)
        && DateTime.TryParse(
            fechaTerminoTexto,
            out var fechaTerminoParseada
        )
    )
    {
        fechaTermino = fechaTerminoParseada.Date;
    }

    // Placas
    placas = ObtenerTexto("placas2433");

    // Hora de recepción
    horaRecepcion = ObtenerTexto("horaRecepcion");

    // Hora de término
    horaTermino = ObtenerTexto("horaTerminoRecepcion");

    // Número de sellos
    numeroSellos = ObtenerTexto("numeroSellos2433");

    // Número de factura
    numeroFactura = ObtenerTexto("numeroFactura2433");

    // Turno
    turno = ObtenerTexto("turno2433");

    // Tripulación
    tripulacion = ObtenerTexto("tripulacion2433");

    // Guardar datos generales en SQL Server
    await _checklistRepository.GuardarDatosQuimicosAsync(
        idInspeccion.Value,
        producto,
        fechaRecepcion,
        fechaTermino,
        placas,
        horaRecepcion,
        horaTermino,
        numeroSellos,
        numeroFactura,
        turno,
        tripulacion
    );

// GUARDAR Y RELACIONAR OPERADOR DE SG-F-24-33

string? nombreOperador = ObtenerTexto("operador2433");

if (
    !string.IsNullOrWhiteSpace(nombreOperador)
    && idInspeccion.HasValue
)
{
    int idOperador =
        await _checklistRepository.ObtenerOCrearOperadorAsync(
            nombreOperador
        );

    await _checklistRepository.RelacionarOperadorConInspeccionAsync(
        idInspeccion.Value,
        idOperador
    );

    Console.WriteLine(
        $"Operador guardado y relacionado. ID operador: {idOperador}"
    );
}

    Console.WriteLine(
        "Datos generales de SG-F-24-33 guardados en SQL."
    );

// =========================================================
// GUARDAR NIVEL DEL TANQUE DE SG-F-24-33
// =========================================================

string? nivelAntes = ObtenerTexto("nivelAntes");
string? nivelDespues = ObtenerTexto("nivelDespues");

if (idInspeccion.HasValue)
{
    await _checklistRepository.GuardarNivelTanqueAsync(
        idInspeccion.Value,
        nivelAntes,
        nivelDespues
    );

    Console.WriteLine(
        "Niveles del tanque guardados en SQL."
    );
}

// =========================================================
// GUARDAR CONDICIONES DE SEGURIDAD
// =========================================================

bool ObtenerBooleano(string nombrePropiedad)
{
    if (
        documento.RootElement.TryGetProperty(
            nombrePropiedad,
            out var elemento
        )
        && (
            elemento.ValueKind == JsonValueKind.True ||
            elemento.ValueKind == JsonValueKind.False
        )
    )
    {
        return elemento.GetBoolean();
    }

    return false;
}

bool conosSeguridad = ObtenerBooleano("conosSeguridad");
bool ventilarOperacion = ObtenerBooleano("ventilarOperacion");
bool contenedorIdentificado = ObtenerBooleano("contenedorIdentificado");
bool identificacionNOM = ObtenerBooleano("identificacionNOM");

await _checklistRepository.GuardarRespuestaCondicionAsync(
    idInspeccion.Value,
    "CONOS_SEGURIDAD",
    conosSeguridad
);

await _checklistRepository.GuardarRespuestaCondicionAsync(
    idInspeccion.Value,
    "VENTILAR_OPERACION",
    ventilarOperacion
);

await _checklistRepository.GuardarRespuestaCondicionAsync(
    idInspeccion.Value,
    "CONTENEDOR_IDENTIFICADO",
    contenedorIdentificado
);

await _checklistRepository.GuardarRespuestaCondicionAsync(
    idInspeccion.Value,
    "IDENTIFICACION_NOM",
    identificacionNOM
);

Console.WriteLine(
    "Condiciones de seguridad guardadas en SQL."
);

// =========================================================
// GUARDAR GRADOS DE RIESGO
// CHECKLIST: SG-F-24-33
// =========================================================
// Los campos de riesgo llegan desde React como booleanos:
//
// explosivo
// inflamable
// gasPresion
// corrosivo
// comburente
// toxicidad
// salud
// medioAmbiente
//
// true  = riesgo seleccionado
// false = riesgo no seleccionado
//
// Los riesgos seleccionados se guardan en:
// [userchecklist].[GRADO_RIESGO]
//
// No se guarda un registro para los riesgos no seleccionados.
// =========================================================

// ---------------------------------------------------------
// Relacionar cada campo de React con el nombre que se
// almacenará en GRADO_RIESGO.tipo_riesgo.
// ---------------------------------------------------------

var gradosRiesgo2433 = new[]
{
    new
    {
        Campo = "explosivo",
        TipoRiesgo = "Explosivo"
    },
    new
    {
        Campo = "inflamable",
        TipoRiesgo = "Inflamable"
    },
    new
    {
        Campo = "gasPresion",
        TipoRiesgo = "Gas a presión"
    },
    new
    {
        Campo = "corrosivo",
        TipoRiesgo = "Corrosivo"
    },
    new
    {
        Campo = "comburente",
        TipoRiesgo = "Comburente"
    },
    new
    {
        Campo = "toxicidad",
        TipoRiesgo = "Toxicidad"
    },
    new
    {
        Campo = "salud",
        TipoRiesgo = "Salud"
    },
    new
    {
        Campo = "medioAmbiente",
        TipoRiesgo = "Medio ambiente"
    }
};

// ---------------------------------------------------------
// Revisar cada riesgo y guardar únicamente los seleccionados.
// ---------------------------------------------------------

foreach (var riesgo in gradosRiesgo2433)
{
    bool seleccionado = ObtenerBooleano(riesgo.Campo);

    if (seleccionado)
    {
        await _checklistRepository.GuardarGradoRiesgoAsync(
            idInspeccion.Value, 
            riesgo.TipoRiesgo
        );
    }
}

Console.WriteLine(
    "Grados de riesgo guardados en SQL."
);

// =========================================================
// GUARDAR CADUCIDAD DE SG-F-24-33
// =========================================================

for (int i = 1; i <= 4; i++)
{
    string nombreProducto = $"producto{i}";
    string nombreCaducidad = $"caducidad{i}";

    string? productoCaducidad = ObtenerTexto(nombreProducto);
    string? fechaCaducidadTexto = ObtenerTexto(nombreCaducidad);

    if (
        !string.IsNullOrWhiteSpace(productoCaducidad)
        && !string.IsNullOrWhiteSpace(fechaCaducidadTexto)
        && DateTime.TryParse(
            fechaCaducidadTexto,
            out var fechaCaducidad
        )
    )
    {
        await _checklistRepository.GuardarCaducidadAsync(
            idInspeccion.Value,
            productoCaducidad,
            fechaCaducidad
        );
    }
}

Console.WriteLine(
    "Caducidades guardadas en SQL."
);

// =========================================================
// GUARDAR CONDICIONES DEL MATERIAL
// CHECKLIST: SG-F-24-33
// =========================================================
// Este bloque guarda las respuestas de las preguntas:
// - CANTIDAD_FACTURA
// - CERTIFICADO_CALIDAD
// - CONTENEDORES_CONDICIONES
//
// También guarda las observaciones correspondientes.
//
// Las respuestas válidas son:
// - CUMPLE
// - NO_CUMPLE
// - NA
// =========================================================

string? ObtenerValorRespuesta(string nombrePropiedad)
{
    if (
        documento.RootElement.TryGetProperty(
            nombrePropiedad,
            out var elemento
        )
        && elemento.ValueKind == JsonValueKind.String
    )
    {
        return elemento.GetString();
    }

    return null;
}

string? cantidadFactura =
    ObtenerValorRespuesta("cantidadFactura");

string? cantidadFacturaObs =
    ObtenerValorRespuesta("cantidadFacturaObs");

string? certificadoCalidad =
    ObtenerValorRespuesta("certificadoCalidad");

string? certificadoCalidadObs =
    ObtenerValorRespuesta("certificadoCalidadObs");

string? contenedoresBuenasCondiciones =
    ObtenerValorRespuesta("contenedoresBuenasCondiciones");

string? contenedoresBuenasCondicionesObs =
    ObtenerValorRespuesta("contenedoresBuenasCondicionesObs");

if (
    idInspeccion.HasValue
)
{
    if (!string.IsNullOrWhiteSpace(cantidadFactura))
    {
        await _checklistRepository.GuardarRespuestaOpcionAsync(
            idInspeccion.Value,
            "CANTIDAD_FACTURA",
            cantidadFactura,
            cantidadFacturaObs
        );
    }

    if (!string.IsNullOrWhiteSpace(certificadoCalidad))
    {
        await _checklistRepository.GuardarRespuestaOpcionAsync(
            idInspeccion.Value,
            "CERTIFICADO_CALIDAD",
            certificadoCalidad,
            certificadoCalidadObs
        );
    }

    if (!string.IsNullOrWhiteSpace(contenedoresBuenasCondiciones))
    {
        await _checklistRepository.GuardarRespuestaOpcionAsync(
            idInspeccion.Value,
            "CONTENEDORES_CONDICIONES",
            contenedoresBuenasCondiciones,
            contenedoresBuenasCondicionesObs
        );
    }

    Console.WriteLine(
        "Condiciones del material guardadas en SQL."
    );
 
// GUARDAR EQUIPO DE PROTECCIÓN PERSONAL (EPP)
// CHECKLIST: SG-F-24-33
// SECCIÓN SQL: 23 - Nivel del tanque y condiciones de seguridad
// =========================================================
// Este bloque obtiene las respuestas de los campos:
//
// epp0 hasta epp9
//
// React envía los valores:
// - si
// - no
// - na
//
// SQL Server utiliza:
// - SI
// - NO
// - NA
//
// El método GuardarRespuestaOpcionAsync ya existente se
// reutiliza para guardar cada respuesta en RESPUESTA.
//
// El componente EPP2433.jsx no contiene observaciones,
// por lo que se envía null en ese parámetro.
//
// No modifica ni elimina información existente.
// =========================================================

// ---------------------------------------------------------
// Normalizar los valores enviados desde React
// ---------------------------------------------------------
string? NormalizarOpcionEPP(string? valor)
{
    if (string.IsNullOrWhiteSpace(valor))
    {
        return null;
    }

    return valor.Trim().ToLowerInvariant() switch
    {
        "si" => "SI",
        "no" => "NO",
        "na" => "NA",
        _ => null
    };

    
}

// ---------------------------------------------------------
// Relacionar los campos de React con los códigos de SQL
// ---------------------------------------------------------
// El orden coincide con el arreglo epp2433 del frontend.
// ---------------------------------------------------------

var preguntasEPP = new[]
{
    new
    {
        Campo = "epp0",
        Codigo = "EPP_ARNES_CUERPO_COMPLETO"
    },
    new
    {
        Campo = "epp1",
        Codigo = "EPP_LINEA_VIDA_ANSI"
    },
    new
    {
        Campo = "epp2",
        Codigo = "EPP_LINEA_VIDA_RETRACTIL"
    },
    new
    {
        Campo = "epp3",
        Codigo = "EPP_CASCO_BARBOQUEJO"
    },
    new
    {
        Campo = "epp4",
        Codigo = "EPP_CALZADO_SEGURIDAD"
    },
    new
    {
        Campo = "epp5",
        Codigo = "EPP_GUANTES"
    },
    new
    {
        Campo = "epp6",
        Codigo = "EPP_PROTECCION_AUDITIVA"
    },
    new
    {
        Campo = "epp7",
        Codigo = "EPP_PROTECCION_VISUAL"
    },
    new
    {
        Campo = "epp8",
        Codigo = "EPP_PROTECCION_RESPIRATORIA"
    },
    new
    {
        Campo = "epp9",
        Codigo = "EPP_PROTECCION_CORPORAL"
    }
};

// ---------------------------------------------------------
// Guardar las respuestas seleccionadas
// ---------------------------------------------------------

foreach (var pregunta in preguntasEPP)
{
    string? valorReact =
        ObtenerTexto(pregunta.Campo);

    string? valorSQL =
        NormalizarOpcionEPP(valorReact);

    // Solo se guarda si el usuario seleccionó una opción
    // válida en el componente React.
    if (!string.IsNullOrWhiteSpace(valorSQL))
    {
        await _checklistRepository.GuardarRespuestaOpcionAsync(
            idInspeccion.Value,
            pregunta.Codigo,
            valorSQL,
            null
        );
    }
}

Console.WriteLine(
    "Equipo de Protección Personal guardado en SQL."
);

// =========================================================
// GUARDAR TRASVASE
// CHECKLIST: SG-F-24-33
// SECCIÓN SQL: 25 - Trasvase
// =========================================================
// Este bloque obtiene las 15 respuestas del componente
// Trasvase2433.jsx:
//
// antes0  hasta antes4
// durante0 hasta durante4
// despues0 hasta despues4
//
// React envía:
// - si
// - no
// - na
//
// SQL Server utiliza:
// - SI
// - NO
// - NA
//
// Se reutiliza GuardarRespuestaOpcionAsync().
// El componente no contiene campos de observaciones,
// por lo que se envía null.
//
// El campo tipoTrabajo se manejará por separado porque
// representa el tipo de actividad y no una respuesta SI/NO/NA.
// =========================================================

// ---------------------------------------------------------
// Normalizar los valores enviados desde React
// ---------------------------------------------------------

string? NormalizarOpcionTrasvase(string? valor)
{
    if (string.IsNullOrWhiteSpace(valor))
    {
        return null;
    }

    return valor.Trim().ToLowerInvariant() switch
    {
        "si" => "SI",
        "no" => "NO",
        "na" => "NA",
        _ => null
    };
}

// ---------------------------------------------------------
// Relacionar los campos de React con los códigos SQL
// ---------------------------------------------------------

var preguntasTrasvase = new[]
{
    new
    {
        Campo = "antes0",
        Codigo = "TRASVASE_AREA_DELIMITADA"
    },
    new
    {
        Campo = "antes1",
        Codigo = "TRASVASE_EXTINTOR"
    },
    new
    {
        Campo = "antes2",
        Codigo = "TRASVASE_EPP_COMPLETO"
    },
    new
    {
        Campo = "antes3",
        Codigo = "TRASVASE_HOJA_SEGURIDAD"
    },
    new
    {
        Campo = "antes4",
        Codigo = "TRASVASE_KIT_DERRAMES"
    },
    new
    {
        Campo = "durante0",
        Codigo = "TRASVASE_NO_FUGAS"
    },
    new
    {
        Campo = "durante1",
        Codigo = "TRASVASE_COMUNICACION"
    },
    new
    {
        Campo = "durante2",
        Codigo = "TRASVASE_NIVEL_TANQUE"
    },
    new
    {
        Campo = "durante3",
        Codigo = "TRASVASE_NO_IGNICION"
    },
    new
    {
        Campo = "durante4",
        Codigo = "TRASVASE_AREA_LIBRE"
    },
    new
    {
        Campo = "despues0",
        Codigo = "TRASVASE_AREA_LIMPIA"
    },
    new
    {
        Campo = "despues1",
        Codigo = "TRASVASE_RETIRAR_RESIDUOS"
    },
    new
    {
        Campo = "despues2",
        Codigo = "TRASVASE_RETIRAR_HERRAMIENTAS"
    },
    new
    {
        Campo = "despues3",
        Codigo = "TRASVASE_RETIRAR_SENALAMIENTOS"
    },
    new
    {
        Campo = "despues4",
        Codigo = "TRASVASE_EQUIPO_ALMACENADO"
    }
};

// ---------------------------------------------------------
// Guardar las respuestas seleccionadas
// ---------------------------------------------------------

foreach (var pregunta in preguntasTrasvase)
{
    string? valorReact =
        ObtenerTexto(pregunta.Campo);

    string? valorSQL =
        NormalizarOpcionTrasvase(valorReact);

    // Solo se guarda cuando existe una respuesta válida.
    if (!string.IsNullOrWhiteSpace(valorSQL))
    {
        await _checklistRepository.GuardarRespuestaOpcionAsync(
            idInspeccion.Value,
            pregunta.Codigo,
            valorSQL,
            null
        );
    }
}

Console.WriteLine(
    "Trasvase guardado en SQL."
);

// =========================================================
// GUARDAR TIPO DE ACTIVIDAD DE TRASVASE
// =========================================================
// Obtiene el tipo de trabajo seleccionado en React
// y lo guarda en ACTIVIDAD_TRASVASE relacionado
// con la inspección actual.
//
// Valores esperados desde React:
// preventivo
// trasvase
// correctivo
// modificacion
//
// Compatible con SQL Server 2016.
// =========================================================

string? tipoTrabajo = ObtenerTexto("tipoTrabajo");

if (!string.IsNullOrWhiteSpace(tipoTrabajo))
{
    string tipoActividad = tipoTrabajo.Trim().ToLowerInvariant();

    switch (tipoActividad)
    {
        case "preventivo":
        case "trasvase":
        case "correctivo":
        case "modificacion":

            await _checklistRepository.GuardarActividadTrasvaseAsync(
                idInspeccion.Value,
                tipoActividad
            );

            Console.WriteLine(
                $"Tipo de actividad de Trasvase guardado: {tipoActividad}"
            );

            break;

        default:

            Console.WriteLine(
                $"Tipo de actividad no reconocido: {tipoTrabajo}"
            );

            break;
    }
}
else
{
    Console.WriteLine(
        "No se recibió tipoTrabajo para el checklist SG-F-24-33."
    );
}

// =========================================================
// GUARDAR PELIGROS Y ASPECTOS AMBIENTALES
// CHECKLIST: SG-F-24-33
// =========================================================
// Los campos peligro0...peligro23 y ambiental0...ambiental11
// llegan desde React como valores booleanos.
//
// true  = seleccionado
// false = no seleccionado
//
// Se guarda cada elemento junto con la inspección actual.
// Compatible con SQL Server 2016.
// =========================================================

var peligros2433 = new[]
{
    "Deficiencia de oxígeno",
    "Carga manual",
    "Movimiento de cargas",
    "Colisión o volcadura",
    "Partes en movimiento",
    "Exposición a ruido",
    "Posturas",
    "Proyección de partículas",
    "Superficies resbalosas",
    "Caídas al mismo nivel",
    "Contacto con sustancias químicas",
    "Incendio",
    "Caídas de diferente nivel",
    "Invasión",
    "Atropellamiento",
    "Explosión",
    "Contacto con superficies calientes",
    "Riesgos eléctricos",
    "Polvo",
    "Iluminación no adecuada",
    "Cuerpos punzocortantes",
    "Vibración",
    "Aves",
    "Agotamiento físico"
};

for (int i = 0; i < peligros2433.Length; i++)
{
    string nombreCampo = $"peligro{i}";

    if (
        documento.RootElement.TryGetProperty(
            nombreCampo,
            out var elemento
        )
        && (
            elemento.ValueKind == JsonValueKind.True
            || elemento.ValueKind == JsonValueKind.False
        )
    )
    {
        bool seleccionado = elemento.GetBoolean();

        await _checklistRepository.GuardarPeligroAsync(
            idInspeccion.Value,
            peligros2433[i],
            seleccionado
        );
    }
}

Console.WriteLine("Peligros ambientales guardados en SQL.");


// =========================================================
// GUARDAR ASPECTOS AMBIENTALES
// =========================================================
// Los campos ambiental0...ambiental11 llegan desde React
// como valores booleanos y se almacenan como BIT.
// =========================================================

var aspectosAmbientales2433 = new[]
{
    "Generación de residuos peligrosos",
    "Generación de ruido",
    "Consumo de energía",
    "Consumo de agua",
    "Generación de calor",
    "Consumo de productos químicos",
    "Consumo de materiales",
    "Descarga de aguas",
    "Emisiones a la atmósfera",
    "Generación de residuos no peligrosos",
    "Contaminación del agua",
    "Contaminación del suelo"
};

for (int i = 0; i < aspectosAmbientales2433.Length; i++)
{
    string nombreCampo = $"ambiental{i}";

    if (
        documento.RootElement.TryGetProperty(
            nombreCampo,
            out var elemento
        )
        && (
            elemento.ValueKind == JsonValueKind.True
            || elemento.ValueKind == JsonValueKind.False
        )
    )
    {
        bool seleccionado = elemento.GetBoolean();

        await _checklistRepository.GuardarAspectoAmbientalAsync(
            idInspeccion.Value,
            aspectosAmbientales2433[i],
            seleccionado
        );
    }
}

Console.WriteLine("Aspectos ambientales guardados en SQL.");

}

// =========================================================
// GUARDAR SEGURIDAD DEL TRANSPORTE
// CHECKLIST: SG-F-24-33
// SECCIÓN SQL: 21 - Calidad del Servicio del Transportista
// =========================================================
// Este bloque obtiene las respuestas de los campos:
//
// transporte0  hasta transporte10
//
// También obtiene sus observaciones:
//
// transporteObs0 hasta transporteObs10
//
// React envía:
// - cumple
// - nocumple
// - na
//
// SQL Server utiliza:
// - CUMPLE
// - NO_CUMPLE
// - NA
//
// Por eso se normalizan los valores antes de enviarlos
// al método GuardarRespuestaOpcionAsync.
//
// No modifica ni elimina información existente.
// =========================================================

// ---------------------------------------------------------
// Convertir el valor de React al valor utilizado en SQL
// ---------------------------------------------------------
string? NormalizarOpcionTransporte(string? valor)
{
    if (string.IsNullOrWhiteSpace(valor))
    {
        return null;
    }

    return valor.Trim().ToLowerInvariant() switch
    {
        "cumple" => "CUMPLE",
        "nocumple" => "NO_CUMPLE",
        "no_cumple" => "NO_CUMPLE",
        "na" => "NA",
        _ => null
    };
}

// ---------------------------------------------------------
// Relacionar cada campo de React con su código SQL
// ---------------------------------------------------------
// El arreglo conserva el mismo orden que transporte2433
// en el frontend.
//
// Cada elemento contiene:
// [0] = nombre del campo en React
// [1] = código de la pregunta en SQL Server
// [2] = nombre del campo de observaciones
// ---------------------------------------------------------

var preguntasTransporte = new[]
{
    new
    {
        Campo = "transporte0",
        Codigo = "TRANSPORTE_LIMPIO_PLAGAS",
        Observacion = "transporteObs0"
    },
    new
    {
        Campo = "transporte1",
        Codigo = "TRANSP_LIBRE_CONTAMINANTES",
        Observacion = "transporteObs1"
    },
    new
    {
        Campo = "transporte2",
        Codigo = "TIPO_TRANSPORTE",
        Observacion = "transporteObs2"
    },
    new
    {
        Campo = "transporte3",
        Codigo = "COND_GEN_TRANSPORTE",
        Observacion = "transporteObs3"
    },
    new
    {
        Campo = "transporte4",
        Codigo = "ATERRIZAR_UNIDAD",
        Observacion = "transporteObs4"
    },
    new
    {
        Campo = "transporte5",
        Codigo = "IDENTIFICACION_TRANSPORTE",
        Observacion = "transporteObs5"
    },
    new
    {
        Campo = "transporte6",
        Codigo = "ACORDONAMIENTO_AREA",
        Observacion = "transporteObs6"
    },
    new
    {
        Campo = "transporte7",
        Codigo = "HOJA_SEGURIDAD_TRANSPORTE",
        Observacion = "transporteObs7"
    },
    new
    {
        Campo = "transporte8",
        Codigo = "KIT_DERRAMES_TRANSPORTE",
        Observacion = "transporteObs8"
    },
    new
    {
        Campo = "transporte9",
        Codigo = "RAMPA_MOVIL_CONDICIONES",
        Observacion = "transporteObs9"
    },
    new
    {
        Campo = "transporte10",
        Codigo = "FUENTES_IGNICION_TRANSPORTE",
        Observacion = "transporteObs10"
    }
};

    // Guardar las respuestas seleccionadas

    foreach (var pregunta in preguntasTransporte)
    {
        string? valorReact =
            ObtenerTexto(pregunta.Campo);

        string? valorSQL =
            NormalizarOpcionTransporte(valorReact);

        string? observaciones =
            ObtenerTexto(pregunta.Observacion);

        // Solo se guarda si el usuario seleccionó una opción
        // válida en el frontend.
        if (!string.IsNullOrWhiteSpace(valorSQL))
        {
        await _checklistRepository.GuardarRespuestaOpcionAsync(
            idInspeccion.Value,
            pregunta.Codigo,
            valorSQL,
            observaciones
        );
    }
}

Console.WriteLine(
    "Seguridad del transporte guardada en SQL."
);

// =========================================================
// GUARDAR LLANTAS DEL CHECKLIST DE TRANSPORTE
// =========================================================
// Este bloque guarda el estado de cada llanta y sus
// incidencias asociadas.
//
// Frontend:
// - llantasSencillo
// - llantasFull
//
// Cada llanta contiene:
// - id
// - numero
// - estado
// - incidencias[]
// - comentario
//
// Base de datos:
// - LLANTA_INSPECCION
// - LLANTA_INCIDENCIA
// Las llantas FULL solamente se guardan cuando existe un segundo remolque.

bool esTransporte =
    tipoChecklist?.Trim().Equals(
        "CHK-TRANSPORTE",
        StringComparison.OrdinalIgnoreCase
    ) == true;

bool tieneId =
    idInspeccion.HasValue;

Console.WriteLine($"*** esTransporte = {esTransporte} ***");
Console.WriteLine($"*** tieneId = {tieneId} ***");

if (esTransporte && tieneId)
{
    // Función local para guardar las llantas de un remolque

    async Task GuardarLlantasAsync(
        JsonElement arregloLlantas,
        string tipoRemolque)
    {
        if (arregloLlantas.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        Console.WriteLine(
            $"*** ARREGLO DE LLANTAS RECIBIDO: {tipoRemolque} ***"
        );

        Console.WriteLine(
            $"*** CANTIDAD DE LLANTAS: {arregloLlantas.GetArrayLength()} ***"
        );

        foreach (JsonElement llanta in arregloLlantas.EnumerateArray())
        {
            string? numeroLlanta = null;
            string? estado = null;
            string? comentario = null;

        // ---------------------------------------------
        // Número de llanta
        // ---------------------------------------------
        // React puede enviar el número como:
        // - número JSON: 2
        // - texto JSON: "2"
        //
        // Se aceptan ambos formatos para evitar que una
        // diferencia de serialización impida guardar la llanta.
        // ---------------------------------------------
        if (
            llanta.TryGetProperty(
                "numero",
                out var numeroElemento
            )
        )
        {
            if (numeroElemento.ValueKind == JsonValueKind.Number)
            {
                numeroLlanta =
                    numeroElemento.GetRawText();
            }
            else if (numeroElemento.ValueKind == JsonValueKind.String)
            {
                numeroLlanta =
                    numeroElemento.GetString();
            }
        }

            // ---------------------------------------------
            // Estado
            // ---------------------------------------------
            if (
                llanta.TryGetProperty(
                    "estado",
                    out var estadoElemento
                )
                && estadoElemento.ValueKind == JsonValueKind.String
            )
            {
                estado = estadoElemento.GetString();
            }

            // ---------------------------------------------
            // Comentario
            // ---------------------------------------------
            if (
                llanta.TryGetProperty(
                    "comentario",
                    out var comentarioElemento
                )
                && comentarioElemento.ValueKind == JsonValueKind.String
            )
            {
                comentario = comentarioElemento.GetString();
            }

            // ---------------------------------------------
            // Validar información mínima
            // ---------------------------------------------
            if (
                string.IsNullOrWhiteSpace(numeroLlanta)
                || string.IsNullOrWhiteSpace(estado)
            )
            {
                continue;
            }

            // ---------------------------------------------
            // Normalizar valores
            // ---------------------------------------------
            string tipoRemolqueNormalizado =
                tipoRemolque.Trim().ToUpperInvariant();

            string estadoNormalizado =
                estado.Trim().ToUpperInvariant();

            // ---------------------------------------------
            // Guardar llanta principal
            // ---------------------------------------------

            Console.WriteLine(
            $"*** GUARDANDO LLANTA: {tipoRemolqueNormalizado} - NUMERO: [{numeroLlanta}] - ESTADO: [{estadoNormalizado}] ***"
        );
            int idLlanta =
                await _checklistRepository.GuardarLlantaAsync(
                    idInspeccion.Value,
                    tipoRemolqueNormalizado,
                    numeroLlanta.Trim(),
                    estadoNormalizado,
                    string.IsNullOrWhiteSpace(comentario)
                        ? null
                        : comentario.Trim()
                );

            // ---------------------------------------------
            // Guardar incidencias
            //
            // Solo se consideran incidencias cuando la llanta
            // actualmente está marcada como DANADA.
            // ---------------------------------------------
            if (
                estadoNormalizado == "DANADA"
                && llanta.TryGetProperty(
                    "incidencias",
                    out var incidenciasElemento
                )
                && incidenciasElemento.ValueKind == JsonValueKind.Array
            )
            {
                foreach (
                    JsonElement incidenciaElemento
                    in incidenciasElemento.EnumerateArray()
                )
                {
                    if (
                        incidenciaElemento.ValueKind
                        != JsonValueKind.String
                    )
                    {
                        continue;
                    }

                    string? incidencia =
                        incidenciaElemento.GetString();

                    if (string.IsNullOrWhiteSpace(incidencia))
                    {
                        continue;
                    }

                    await _checklistRepository
                        .GuardarIncidenciaLlantaAsync(
                            idLlanta,
                            incidencia.Trim().ToUpperInvariant()
                        );
                }
            }
        }
    }

    // Guardar llantas del remolque sencillo

    if (
    documento.RootElement.TryGetProperty(
        "llantasSencillo",
        out var llantasSencilloElemento
    )
)
{
    Console.WriteLine(
        $"*** LLANTAS SENCILLO - TIPO JSON: {llantasSencilloElemento.ValueKind} ***"
    );

    Console.WriteLine(
        $"*** LLANTAS SENCILLO - CONTENIDO: {llantasSencilloElemento.GetRawText()} ***"
    ); 

    await GuardarLlantasAsync(
        llantasSencilloElemento,
        "SENCILLO"
    );
}
    // Guardar llantas FULL solamente cuando existe un segundo remolque.
    
    bool tieneSegundoRemolque = false;

    if (
        documento.RootElement.TryGetProperty(
            "remolque2",
            out var remolque2Elemento
        )
        && remolque2Elemento.ValueKind == JsonValueKind.String
    )
    {
        tieneSegundoRemolque =
            !string.IsNullOrWhiteSpace(
                remolque2Elemento.GetString()
            );
    }

    if (
        tieneSegundoRemolque
        && documento.RootElement.TryGetProperty(
            "llantasFull",
            out var llantasFullElemento
        )
    )
    {
        await GuardarLlantasAsync(
            llantasFullElemento,
            "FULL"
        );
    }

    Console.WriteLine(
        "Llantas del checklist de transporte guardadas en SQL."
    );
}
}

// -----------------------------------------------------
// Crear carpeta principal del checklist
// -----------------------------------------------------

string carpetaBase;

if (tipoChecklist == "CHK-TRANSPORTE")
{
    DateTime fechaActual = DateTime.Now;

    string anio =
        fechaActual.ToString("yyyy");

    string mes =
        $"{fechaActual.Month}." +
        fechaActual.ToString(
            "MMMM",
            new System.Globalization.CultureInfo("es-MX")
        ).ToUpper();

    string dia =
        fechaActual.ToString("dd.MM.yyyy");

    carpetaBase = Path.Combine(
        Directory.GetCurrentDirectory(),
        "Pruebas",
        anio,
        mes,
        dia,
        delivery
    );
}
else
{
    // Los demás checklists permanecen igual
    carpetaBase = Path.Combine(
        Directory.GetCurrentDirectory(),
        "ArchivosChecklist",
        carpetaChecklist,
        folio
    );
}

Directory.CreateDirectory(
    carpetaBase
);


// -----------------------------------------------------
// Crear carpeta de evidencias
// SOLO CHK-TRANSPORTE
// -----------------------------------------------------

string? carpetaEvidencias = null;

if (tipoChecklist == "CHK-TRANSPORTE")
{
    carpetaEvidencias = Path.Combine(
        carpetaBase,
        "Evidencias"
    );

    Directory.CreateDirectory(
        carpetaEvidencias
    );
}

        Console.WriteLine(
            $"Área: {areaMateriaPrima}"
        );

        Console.WriteLine(
            $"Carpeta checklist: {carpetaBase}"
        );

        if (carpetaEvidencias != null)
        {
            Console.WriteLine(
                $"Carpeta evidencias: {carpetaEvidencias}"
            );
        }

            Console.WriteLine(
                $"Carpeta checklist: {carpetaBase}"
            );

            Console.WriteLine(
                $"Carpeta evidencias: {carpetaEvidencias}"
            );

            // =====================================================
            // GUARDAR EVIDENCIAS
            // =====================================================

            int evidenciasGuardadas = 0;

            if (
                evidencias != null &&
                evidencias.Count > 0 &&
                carpetaEvidencias != null
            )
            {
                Console.WriteLine(
                    $"Evidencias recibidas: {evidencias.Count}"
                );

                for (
                    int i = 0;
                    i < evidencias.Count;
                    i++
                )
                {
                    var evidencia = evidencias[i];

                    if (evidencia.Length <= 0)
                    {
                        continue;
                    }

                    string extension =
                        Path.GetExtension(
                            evidencia.FileName
                        );

                    if (
                        string.IsNullOrWhiteSpace(
                            extension
                        )
                    )
                    {
                        extension = ".jpg";
                    }

                    string nombreArchivo =
                        $"{folio}-{i + 1:D2}{extension}";

                    string rutaArchivo =
                        Path.Combine(
                            carpetaEvidencias,
                            nombreArchivo
                        );

                    using (
                        var stream =
                            new FileStream(
                                rutaArchivo,
                                FileMode.Create
                            )
                    )
                    {
                        await evidencia.CopyToAsync(
                            stream
                        );
                    }

// -----------------------------------------------------
// SUBIR EVIDENCIA AL FTP
// -----------------------------------------------------
Console.WriteLine(
    $"TIPO CHECKLIST ANTES DE FTP: [{tipoChecklist}]"
);

if (tipoChecklist == "CHK-TRANSPORTE")
{
    string carpetaPruebasLocal =
        Path.Combine(
            Directory.GetCurrentDirectory(),
            "Pruebas"
        );

    string rutaRelativa =
        Path.GetRelativePath(
            carpetaPruebasLocal,
            carpetaBase
        );

    string rutaRemota =
        "/Pruebas/" +
        rutaRelativa.Replace("\\", "/") +
        "/Evidencias/" +
        nombreArchivo;

    await _ftpService.SubirArchivoAsync(
        rutaArchivo,
        rutaRemota
    );

    Console.WriteLine(
        $"Evidencia subida al FTP: {rutaRemota}"
    );
}

                    evidenciasGuardadas++;

                    Console.WriteLine(
                        $"Evidencia guardada: {nombreArchivo}"
                    );
                }
            }
            else
            {
                Console.WriteLine(
                    "No se recibieron evidencias."
                );
            }

            // =====================================================
            // RESPUESTA
            // =====================================================

            return Ok(new
            {
                mensaje =
                    "Checklist recibido correctamente",

                folio = folio,

                checklistGuardado = true,

                evidenciasRecibidas =
                    evidencias?.Count ?? 0,

                evidenciasGuardadas =
                    evidenciasGuardadas
            });
        }


// =========================================================
// GUARDAR PDF
// =========================================================

[HttpPost("pdf")]
public async Task<IActionResult> GuardarPDF(
    [FromForm] string folio,
    [FromForm] string? areaMateriaPrima,
    [FromForm] string tipoChecklist,
    [FromForm] string? delivery,
    [FromForm] string? material,
    [FromForm] string? facturaRemision,
    [FromForm] IFormFile pdf)
{
    Console.WriteLine("=================================");
    Console.WriteLine("PDF RECIBIDO");

    Console.WriteLine(
        $"TIPO CHECKLIST PDF: [{tipoChecklist}]"
    );

    // -----------------------------------------------------
    // Validar folio
    // -----------------------------------------------------

    if (string.IsNullOrWhiteSpace(folio))
    {
        return BadRequest(new
        {
            mensaje = "No se recibió el folio."
        });
    }

    // -----------------------------------------------------
    // Validar PDF
    // -----------------------------------------------------

    if (pdf == null || pdf.Length == 0)
    {
        return BadRequest(new
        {
            mensaje = "No se recibió el archivo PDF."
        });
    }

    // -----------------------------------------------------
    // Evitar caracteres/rutas no deseadas
    // -----------------------------------------------------

    folio = Path.GetFileName(folio);

    // -----------------------------------------------------
    // Obtener número de DELIVERY
    // -----------------------------------------------------

    if (string.IsNullOrWhiteSpace(delivery))
    {
        delivery = "SIN-DELIVERY";
    }

    delivery = Path.GetFileName(delivery);

    Console.WriteLine(
        $"Área PDF: {areaMateriaPrima}"
    );

    Console.WriteLine(
        $"Delivery PDF: {delivery}"
    );

    // =====================================================
    // CHK-TRANSPORTE
    // SUBIR PDF DIRECTAMENTE AL FTP
    // SIN CREAR COPIA LOCAL
    // =====================================================

    if (
        tipoChecklist?.Trim().Equals(
            "CHK-TRANSPORTE",
            StringComparison.OrdinalIgnoreCase
        ) == true
    )
    {
        // -------------------------------------------------
        // Fecha actual
        // -------------------------------------------------

        DateTime fechaActual = DateTime.Now;

        string anio =
            fechaActual.ToString("yyyy");

        string mes =
            $"{fechaActual.Month}." +
            fechaActual.ToString(
                "MMMM",
                new System.Globalization.CultureInfo("es-MX")
            ).ToUpper();

        string dia =
            fechaActual.ToString(
                "dd.MM.yyyy"
            );

        // -------------------------------------------------
        // Nombre del PDF
        // -------------------------------------------------

        string nombreArchivo =
            $"{folio}.pdf";

        // -------------------------------------------------
        // Ruta remota del FTP
        // -------------------------------------------------

        string rutaRemota =
            "/Pruebas/" +
            anio +
            "/" +
            mes +
            "/" +
            dia +
            "/" +
            delivery +
            "/" +
            nombreArchivo;

        Console.WriteLine(
            "================================="
        );

        Console.WriteLine(
            $"RUTA FTP PDF: {rutaRemota}"
        );

        // -------------------------------------------------
        // Subir directamente desde el stream
        // -------------------------------------------------

        await using Stream stream =
            pdf.OpenReadStream();

        await _ftpService.SubirStreamAsync(
            stream,
            rutaRemota
        );

        Console.WriteLine(
            $"PDF subido directamente al FTP: {rutaRemota}"
        );

        Console.WriteLine(
            $"Folio PDF: {folio}"
        );

        Console.WriteLine(
            $"Tamaño: {pdf.Length} bytes"
        );

        Console.WriteLine(
            "================================="
        );

        return Ok(new
        {
            mensaje =
                "PDF subido correctamente al FTP",

            folio =
                folio,

            archivo =
                nombreArchivo,

            rutaFTP =
                rutaRemota
        });
    }

    // =====================================================
// SG-F-24-01
// SUBIR PDF DIRECTAMENTE AL FTP
// =====================================================

if (
    tipoChecklist?.Trim().Equals(
        "SG-F-24-01",
        StringComparison.OrdinalIgnoreCase
    ) == true
)
{
    // -------------------------------------------------
    // Validar área
    // -------------------------------------------------

    if (string.IsNullOrWhiteSpace(areaMateriaPrima))
    {
        return BadRequest(new
        {
            mensaje = "No se recibió el área de SG-F-24-01."
        });
    }

    // -------------------------------------------------
    // Validar Factura / Remisión
    // -------------------------------------------------

    if (string.IsNullOrWhiteSpace(facturaRemision))
    {
        return BadRequest(new
        {
            mensaje =
                "No se recibió el número de Factura/Remisión."
        });
    }

    // -------------------------------------------------
    // Fecha actual
    // -------------------------------------------------

    DateTime fechaActual = DateTime.Now;

    string anio =
        fechaActual.ToString("yyyy");

    string mes =
        $"{fechaActual.Month}." +
        fechaActual.ToString(
            "MMMM",
            new System.Globalization.CultureInfo("es-MX")
        ).ToUpper();

    string dia =
        fechaActual.ToString(
            "dd.MM.yyyy"
        );

    // -------------------------------------------------
    // Normalizar texto para carpeta FTP
    // -------------------------------------------------

    string NormalizarNombreFTP(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return "";
        }

        string textoNormalizado =
            texto.Trim().Normalize(
                System.Text.NormalizationForm.FormD
            );

        var resultado =
            new System.Text.StringBuilder();

        foreach (char caracter in textoNormalizado)
        {
            var categoria =
                System.Globalization.CharUnicodeInfo
                    .GetUnicodeCategory(caracter);

            if (
                categoria ==
                System.Globalization.UnicodeCategory.NonSpacingMark
            )
            {
                continue;
            }

            if (
                char.IsLetterOrDigit(caracter) ||
                caracter == ' ' ||
                caracter == '-' ||
                caracter == '_'
            )
            {
                resultado.Append(caracter);
            }
        }

        return resultado
            .ToString()
            .Normalize(
                System.Text.NormalizationForm.FormC
            )
            .Trim()
            .ToUpperInvariant();
    }

    // -------------------------------------------------
    // Nombre del archivo
    // -------------------------------------------------

    string nombreFacturaRemision =
        Path.GetFileName(
            facturaRemision.Trim()
        );

    if (
        string.IsNullOrWhiteSpace(
            nombreFacturaRemision
        )
    )
    {
        return BadRequest(new
        {
            mensaje =
                "El número de Factura/Remisión no es válido."
        });
    }

    string nombreArchivo =
        $"{nombreFacturaRemision}.pdf";

    // -------------------------------------------------
    // Carpeta principal
    // -------------------------------------------------

    string carpetaSGF2401;

if (
    areaMateriaPrima.Trim().Equals(
        "Lata Vacía",
        StringComparison.OrdinalIgnoreCase
    )
)
{
    carpetaSGF2401 =
        "LISTA CHEQUEO SG-F-24-01 LATA VACIA";
}
else if (
    areaMateriaPrima.Trim().Equals(
        "Materias Primas",
        StringComparison.OrdinalIgnoreCase
    )

    )
    {
        carpetaSGF2401 =
            "LISTA CHEQUEO SG-F-24-01 MATERIAS PRIMAS";
    }
    else
    {
        return BadRequest(new
        {
            mensaje =
                $"Área no válida para SG-F-24-01: {areaMateriaPrima}"
        });
    }

    // -------------------------------------------------
    // Construir ruta FTP
    // -------------------------------------------------

    string rutaRemota =
        "/CHECK LIST/" +
        carpetaSGF2401 +
        "/" +
        mes +
        "/" +
        dia +
        "/";

    // -------------------------------------------------
    // Materias Primas necesita carpeta de material
    // -------------------------------------------------

    if (
        areaMateriaPrima.Trim().Equals(
            "Materias Primas",
            StringComparison.OrdinalIgnoreCase
        )
    )
    {
        if (string.IsNullOrWhiteSpace(material))
        {
            return BadRequest(new
            {
                mensaje =
                    "No se recibió el material de SG-F-24-01."
            });
        }

        string materialFTP =
            NormalizarNombreFTP(material);

        if (string.IsNullOrWhiteSpace(materialFTP))
        {
            return BadRequest(new
            {
                mensaje =
                    "El material recibido no es válido."
            });
        }

        rutaRemota +=
            materialFTP +
            "/";
    }

    // -------------------------------------------------
    // Agregar nombre del PDF
    // -------------------------------------------------

    rutaRemota +=
        nombreArchivo;

    Console.WriteLine(
        "================================="
    );

    Console.WriteLine(
        "SG-F-24-01 - PDF FTP"
    );

    Console.WriteLine(
        $"Área: {areaMateriaPrima}"
    );

    Console.WriteLine(
        $"Material: {material}"
    );

    Console.WriteLine(
        $"Factura/Remisión: {facturaRemision}"
    );

    Console.WriteLine(
        $"Ruta FTP: {rutaRemota}"
    );

    // -------------------------------------------------
    // Subir directamente al FTP
    // -------------------------------------------------

    await using Stream stream =
        pdf.OpenReadStream();

    await _ftpService.SubirStreamAsync(
        stream,
        rutaRemota
    );

    Console.WriteLine(
        $"PDF SG-F-24-01 subido al FTP: {rutaRemota}"
    );

    Console.WriteLine(
        $"Tamaño: {pdf.Length} bytes"
    );

    Console.WriteLine(
        "================================="
    );

    return Ok(new
    {
        mensaje =
            "PDF SG-F-24-01 subido correctamente al FTP",

        folio =
            folio,

        archivo =
            nombreArchivo,

        rutaFTP =
            rutaRemota
    });
}

    // =====================================================
    // OTROS CHECKLISTS
    // SE CONSERVA EL COMPORTAMIENTO ANTERIOR
    // =====================================================

    string carpetaChecklist;

    if (!string.IsNullOrWhiteSpace(tipoChecklist))
    {
        carpetaChecklist =
            tipoChecklist;
    }
    else
    {
        carpetaChecklist =
            "Otros";
    }

    string carpetaBase =
        Path.Combine(
            Directory.GetCurrentDirectory(),
            "ArchivosChecklist",
            carpetaChecklist,
            folio
        );

    Directory.CreateDirectory(
        carpetaBase
    );

    Console.WriteLine(
        $"Carpeta PDF: {carpetaBase}"
    );

    string nombreArchivoOtros =
        $"{folio}.pdf";

    string rutaPDF =
        Path.Combine(
            carpetaBase,
            nombreArchivoOtros
        );

    using (
        var stream =
            new FileStream(
                rutaPDF,
                FileMode.Create
            )
    )
    {
        await pdf.CopyToAsync(stream);
    }

    Console.WriteLine(
        $"Folio PDF: {folio}"
    );

    Console.WriteLine(
        $"PDF guardado: {nombreArchivoOtros}"
    );

    Console.WriteLine(
        $"Tamaño: {pdf.Length} bytes"
    );

    Console.WriteLine(
        $"Ruta: {rutaPDF}"
    );

    return Ok(new
    {
        mensaje =
            "PDF guardado correctamente",

        folio =
            folio,

        archivo =
            nombreArchivoOtros
    });

        }
    }
}
