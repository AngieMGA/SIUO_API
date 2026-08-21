using SIUO_API.Models;

namespace SIUO_API.Services
{
    public interface IDispositivoRepository
    {
        Task<List<Dispositivo>> ObtenerTodosAsync();

        Task<Dispositivo?> ObtenerPorIdentificadorAsync(
            string identificador
        );

        Task<Dispositivo?> ObtenerPorIdAsync(
            int id
        );

        Task<bool> ActualizarIdentificadorAsync(
            int id,
            string identificador
        );
    }
}