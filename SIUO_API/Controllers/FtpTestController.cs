using Microsoft.AspNetCore.Mvc;
using SIUO_API.Services;
using FluentFTP;

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
public async Task<IActionResult> ProbarFTP()
{
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

    using var cliente =
        new AsyncFtpClient(
            servidor,
            usuario,
            password,
            puerto
        );

    await cliente.Connect();

    var contenidoRaiz =
        await cliente.GetListing("/");

    var contenidoPruebas =
        await cliente.GetListing("/Pruebas");

    var contenidoPruebas2026 =
        await cliente.GetListing("/Pruebas/2026");

    var contenidoSeptiembre =
        await cliente.GetListing(
            "/Pruebas/2026/9.SEPTIEMBRE"
        );

    await cliente.Disconnect();

    return Ok(new
    {
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

       Pruebas2026 = contenidoPruebas2026.Select(x => new
        {
            x.Name,
            x.FullName,
            x.Type
        }),

        Septiembre = contenidoSeptiembre.Select(x => new
        {
            x.Name,
            x.FullName,
            x.Type
        })
    });
}
    }
}