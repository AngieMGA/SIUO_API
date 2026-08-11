namespace SIUO_API.DTOs
{
    public class LoginResponse
    {
        public string Token { get; set; } = "";

        public UsuarioSesion Usuario { get; set; } = new();
    }

    public class UsuarioSesion
    {
        public string Usuario { get; set; } = "";

        public string Nombre { get; set; } = "";

        public string Rol { get; set; } = "";
    }
}