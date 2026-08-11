namespace SIUO_API.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        public string UsuarioNombre { get; set; } = "";

        public string NombreCompleto { get; set; } = "";

        public string PasswordHash { get; set; } = "";

        public string Rol { get; set; } = "";

        public bool Activo { get; set; } = true;
    }
}