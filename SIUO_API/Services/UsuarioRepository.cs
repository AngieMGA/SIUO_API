using SIUO_API.Models;

namespace SIUO_API.Services
{
    public class UsuarioRepository : IUsuarioRepository
    {
        public Task<Usuario?> ObtenerPorUsuarioAsync(string usuario)
        {
            // =====================================================
            // PENDIENTE DE CONECTAR A LA BASE DE DATOS
            //
            // Aquí posteriormente se realizará la consulta:
            //
            // SELECT ...
            // FROM Usuarios
            // WHERE UsuarioNombre = @usuario
            //
            // Por ahora no se devuelve ningún usuario porque
            // todavía no tenemos la BD.
            // =====================================================

            return Task.FromResult<Usuario?>(null);
        }
    }
}