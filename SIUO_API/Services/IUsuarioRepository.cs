using SIUO_API.Models;

namespace SIUO_API.Services
{
    public interface IUsuarioRepository
    {
        Task<Usuario?> ObtenerPorUsuarioAsync(string usuario);
    }
}