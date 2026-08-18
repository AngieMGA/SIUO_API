using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace SIUO_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChecklistController : ControllerBase
    {
        // =========================================================
        // GUARDAR CHECKLIST + JSON + EVIDENCIAS
        // =========================================================

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

            // -----------------------------------------------------
            // Leer el JSON recibido
            // -----------------------------------------------------

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
// Obtener el área del checklist
// -----------------------------------------------------

string? areaMateriaPrima = null;

if (documento.RootElement.TryGetProperty(
    "areaMateriaPrima",
    out var areaElemento))
{
    areaMateriaPrima =
        areaElemento.GetString();
}

// -----------------------------------------------------
// Determinar carpeta según el área
// -----------------------------------------------------

string carpetaArea;

if (areaMateriaPrima == "Lata Vacía")
{
    carpetaArea = "LataVacia";
}
else if (areaMateriaPrima == "Cuarto Monster")
{
    carpetaArea = "CuartoMonster";
}
else
{
    carpetaArea = "Otros";
}

// -----------------------------------------------------
// Crear carpeta principal del checklist
// -----------------------------------------------------

string carpetaBase = Path.Combine(
    Directory.GetCurrentDirectory(),
    "ArchivosChecklist",
    carpetaArea,
    folio
);

Directory.CreateDirectory(
    carpetaBase
);

// -----------------------------------------------------
// Crear carpeta de evidencias
// SOLO para Cuarto Monster
// -----------------------------------------------------

string? carpetaEvidencias = null;

if (areaMateriaPrima == "Cuarto Monster")
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
            // GUARDAR CHECKLIST.JSON
            // =====================================================

            string rutaChecklist = Path.Combine(
                carpetaBase,
                "Checklist.json"
            );

            var opcionesJson =
                new JsonSerializerOptions
                {
                    WriteIndented = true
                };

            string jsonFormateado =
                JsonSerializer.Serialize(
                    documento.RootElement,
                    opcionesJson
                );

            await System.IO.File.WriteAllTextAsync(
                rutaChecklist,
                jsonFormateado
            );

            Console.WriteLine(
                $"Checklist JSON guardado: {rutaChecklist}"
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

                    using var stream =
                        new FileStream(
                            rutaArchivo,
                            FileMode.Create
                        );

                    await evidencia.CopyToAsync(
                        stream
                    );

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
            [FromForm] IFormFile pdf)
        {
            Console.WriteLine("=================================");
            Console.WriteLine("PDF RECIBIDO");

            if (string.IsNullOrWhiteSpace(folio))
            {
                return BadRequest(new
                {
                    mensaje =
                        "No se recibió el folio."
                });
            }

            if (
                pdf == null ||
                pdf.Length == 0
            )
            {
                return BadRequest(new
                {
                    mensaje =
                        "No se recibió el archivo PDF."
                });
            }

            // Evitar caracteres/rutas no deseadas
            folio = Path.GetFileName(folio);

            // -----------------------------------------------------
            // Carpeta del checklist
            // -----------------------------------------------------

            string carpetaBase = Path.Combine(
                Directory.GetCurrentDirectory(),
                "ArchivosChecklist",
                folio
            );

            Directory.CreateDirectory(
                carpetaBase
            );

            // -----------------------------------------------------
            // Nombre del PDF
            // -----------------------------------------------------

            string nombreArchivo =
                $"{folio}.pdf";

            string rutaPDF =
                Path.Combine(
                    carpetaBase,
                    nombreArchivo
                );

            // -----------------------------------------------------
            // Guardar PDF
            // -----------------------------------------------------

            using var stream =
                new FileStream(
                    rutaPDF,
                    FileMode.Create
                );

            await pdf.CopyToAsync(
                stream
            );

            Console.WriteLine(
                $"Folio PDF: {folio}"
            );

            Console.WriteLine(
                $"PDF guardado: {nombreArchivo}"
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

                folio = folio,

                archivo =
                    nombreArchivo
            });
        }
    }
}