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

        if (documento.RootElement.TryGetProperty(
            "comentarios2433",
            out var comentariosElemento))
        {
            observacionesGenerales =
                comentariosElemento.GetString();
        }

        if (documento.RootElement.TryGetProperty(
            "nombreRecibe2433",
            out var recibeElemento))
        {
            nombreRecibe =
                recibeElemento.GetString();
        }

        if (documento.RootElement.TryGetProperty(
            "nombreSupervisor2433",
            out var supervisorElemento))
        {
            nombreSupervisor =
                supervisorElemento.GetString();
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

        string status = "PENDIENTE";

        idInspeccion =
            await _checklistRepository.GuardarInspeccionAsync(
                tipoChecklist,
                folio,
                fechaInspeccion,
                horaInspeccion,
                status,
                observacionesGenerales,
                nombreRecibe,
                nombreSupervisor
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

// =========================================================
// GUARDAR DATOS DE RECEPCIÓN
// CHECKLIST: SG-F-24-01
// =========================================================

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
// ÁREA: Cuarto Monster
// =========================================================

if (
    tipoChecklist?.Trim().Equals(
        "SG-F-24-01",
        StringComparison.OrdinalIgnoreCase
    ) == true
    && idInspeccion.HasValue
    && areaMateriaPrima?.Trim().Equals(
        "Cuarto Monster",
        StringComparison.OrdinalIgnoreCase
    ) == true
)
{
    // ---------------------------------------------------------
    // Obtener material
    // ---------------------------------------------------------

    string? materialCuarto = null;

    if (
        documento.RootElement.TryGetProperty(
            "material",
            out var materialElemento
        )
        && materialElemento.ValueKind == JsonValueKind.String
    )
    {
        materialCuarto = materialElemento.GetString();
    }

    // ---------------------------------------------------------
    // Normalizar respuesta
    // ---------------------------------------------------------

    string? NormalizarRespuestaSGF2401Cuarto(string? valor)
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
    // Preguntas de Cuarto Monster
    // ---------------------------------------------------------

    var preguntasCuartoMonster = new List<string>();

    // =========================================================
    // CONDICIONES DEL TRANSPORTE
    // =========================================================

    preguntasCuartoMonster.AddRange(
        new[]
        {
            "TR-001",
            "TR-002",
            "TR-003",
            "TR-004",
            "TR-005",
            "TR-006",
            "TR-007",
            "TR-008",
            "TR-011"
        }
    );

    // =========================================================
    // CONDICIONES DEL MATERIAL
    // =========================================================

    preguntasCuartoMonster.AddRange(
        new[]
        {
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
            "MAT-013"
        }
    );

    // =========================================================
    // CALIDAD DEL SERVICIO
    // =========================================================

    preguntasCuartoMonster.AddRange(
        new[]
        {
            "SER-001",
            "SER-002",
            "SER-003",
            "SER-004"
        }
    );

    // =========================================================
    // SACO
    // SOLO PARA AZÚCAR
    // =========================================================

    bool esAzucar =
        materialCuarto?.Trim().Equals(
            "Azúcar",
            StringComparison.OrdinalIgnoreCase
        ) == true;

    bool esFructosa =
        materialCuarto?.Trim().Equals(
            "Fructosa 55",
            StringComparison.OrdinalIgnoreCase
        ) == true;

    if (esAzucar)
    {
        preguntasCuartoMonster.AddRange(
            new[]
            {
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

    if (!esAzucar && !esFructosa)
    {
        Console.WriteLine(
            $"Material de Cuarto Monster no reconocido: [{materialCuarto}]"
        );
    }
    else
    {
        // -----------------------------------------------------
        // Guardar respuestas
        // -----------------------------------------------------

        foreach (string codigoPregunta in preguntasCuartoMonster)
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
                    NormalizarRespuestaSGF2401Cuarto(
                        valorReact
                    );

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
                        $"SG-F-24-01 | Cuarto Monster | {materialCuarto} | {codigoPregunta} = {valorSQL}"
                    );
                }
            }
        }

        Console.WriteLine(
            $"Respuestas SG-F-24-01 de Cuarto Monster ({materialCuarto}) guardadas en SQL."
        );
    }
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
