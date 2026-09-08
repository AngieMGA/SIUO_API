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
    }
}