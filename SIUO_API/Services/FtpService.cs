using FluentFTP;
using Microsoft.Extensions.Configuration;

namespace SIUO_API.Services
{
    public class FtpService
    {
        private readonly IConfiguration _configuration;

        public FtpService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // =========================================================
        // SUBIR ARCHIVO LOCAL AL FTP
        // =========================================================

        public async Task SubirArchivoAsync(
            string rutaLocal,
            string rutaRemota)
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

            var resultado =
                await cliente.UploadFile(
                    rutaLocal,
                    rutaRemota,
                    FtpRemoteExists.Overwrite,
                    true
                );

            if (resultado != FtpStatus.Success)
            {
                throw new Exception(
                    $"No se pudo subir el archivo al FTP. Estado: {resultado}"
                );
            }

            await cliente.Disconnect();
        }

        // =========================================================
        // SUBIR STREAM DIRECTAMENTE AL FTP
        // =========================================================
        // Este método permite subir el archivo sin crear primero
        // una copia física en la computadora.
        // =========================================================

        public async Task SubirStreamAsync(
            Stream stream,
            string rutaRemota)
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

            var resultado =
                await cliente.UploadStream(
                    stream,
                    rutaRemota,
                    FtpRemoteExists.Overwrite,
                    true
                );

            if (resultado != FtpStatus.Success)
            {
                throw new Exception(
                    $"No se pudo subir el archivo al FTP. Estado: {resultado}"
                );
            }

            await cliente.Disconnect();
        }
    }
}