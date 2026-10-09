using Microsoft.AspNetCore.Mvc;
using SIUO_API.Services;
using FluentFTP;
using System.Globalization;

namespace SIUO_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FtpTestController : ControllerBase
    {
        private readonly FtpService _ftpService;
        private readonly IConfiguration _configuration;

        public FtpTestController(
            FtpService ftpService,
            IConfiguration configuration)
        {
            _ftpService = ftpService;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> ProbarFTP(
            [FromQuery] string delivery)
        {
           
            // VALIDAR DELIVERY

            if (string.IsNullOrWhiteSpace(delivery))
            {
                return BadRequest(new
                {
                    mensaje = "Debes indicar el delivery."
                });
            }

            // Evitar caracteres/rutas no deseadas
            delivery = Path.GetFileName(delivery);

            // -------------------------------------------------
            // CONFIGURACIÓN FTP
            // -------------------------------------------------

            string servidor =
                _configuration["FTP:Servidor"]
                ?? throw new Exception(
                    "No está configurado el servidor FTP."
                );

            string usuario =
                _configuration["FTP:Usuario"]
                ?? throw new Exception(
                    "No está configurado el usuario FTP."
                );

            string password =
                _configuration["FTP:Password"]
                ?? throw new Exception(
                    "No está configurada la contraseña FTP."
                );

            int puerto =
                _configuration.GetValue<int>(
                    "FTP:Puerto"
                );

            // -------------------------------------------------
            // FECHA AUTOMÁTICA
            // -------------------------------------------------

            DateTime fechaActual = DateTime.Now;

            string anio =
                fechaActual.ToString("yyyy");

            string mes =
                $"{fechaActual.Month}." +
                fechaActual.ToString(
                    "MMMM",
                    new CultureInfo("es-MX")
                ).ToUpper();

            string dia =
                fechaActual.ToString(
                    "dd.MM.yyyy"
                );

            // -------------------------------------------------
            // RUTAS DINÁMICAS
            // -------------------------------------------------

            string rutaDia =
                $"/Pruebas/{anio}/{mes}/{dia}";

            string rutaDelivery =
                $"{rutaDia}/{delivery}";

            string rutaEvidencias =
                $"{rutaDelivery}/Evidencias";

            // -------------------------------------------------
            // CONEXIÓN FTP
            // -------------------------------------------------

            using var cliente =
                new AsyncFtpClient(
                    servidor,
                    usuario,
                    password,
                    puerto
                );

            await cliente.Connect();

            // -------------------------------------------------
            // LISTADOS
            // -------------------------------------------------

            var contenidoRaiz =
                await cliente.GetListing("/");

            var contenidoPruebas =
                await cliente.GetListing(
                    "/Pruebas"
                );

            var contenidoPruebas2026 =
                await cliente.GetListing(
                    "/Pruebas/2026"
                );

            var contenidoMes =
                await cliente.GetListing(
                    $"/Pruebas/2026/{mes}"
                );

            var contenidoDia =
                await cliente.GetListing(
                    rutaDia
                );

            var contenidoDelivery =
                await cliente.GetListing(
                    rutaDelivery
                );

            var contenidoEvidencias =
                await cliente.GetListing(
                    rutaEvidencias
                );

            await cliente.Disconnect();

            // -------------------------------------------------
            // RESPUESTA
            // -------------------------------------------------

            return Ok(new
            {
                FechaActual =
                    fechaActual.ToString(
                        "dd/MM/yyyy HH:mm:ss"
                    ),

                Anio = anio,

                Mes = mes,

                Dia = dia,

                Delivery = delivery,

                RutaDia = rutaDia,

                RutaDelivery = rutaDelivery,

                RutaEvidencias = rutaEvidencias,

                Raiz = contenidoRaiz.Select(x => new
                {
                    x.Name,
                    x.FullName,
                    x.Type
                }),

                Pruebas = contenidoPruebas.Select(x => new
                {
                    x.Name,
                    x.FullName,
                    x.Type
                }),

                Pruebas2026 =
                    contenidoPruebas2026.Select(x => new
                    {
                        x.Name,
                        x.FullName,
                        x.Type
                    }),

                MesFTP =
                    contenidoMes.Select(x => new
                    {
                        x.Name,
                        x.FullName,
                        x.Type
                    }),

                DiaFTP =
                    contenidoDia.Select(x => new
                    {
                        x.Name,
                        x.FullName,
                        x.Type
                    }),

                DeliveryFTP =
                    contenidoDelivery.Select(x => new
                    {
                        x.Name,
                        x.FullName,
                        x.Type
                    }),

                EvidenciasFTP =
                    contenidoEvidencias.Select(x => new
                    {
                        x.Name,
                        x.FullName,
                        x.Type
                    })
            });
        }
    }
}