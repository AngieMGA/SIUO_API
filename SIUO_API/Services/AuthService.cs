using SIUO_API.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SIUO_API.Services
{
    public class AuthService
    {
        private readonly IConfiguration _configuration;
        private readonly IUsuarioRepository _usuarioRepository;

        public AuthService(
            IConfiguration configuration,
            IUsuarioRepository usuarioRepository)
        {
            _configuration = configuration;
            _usuarioRepository = usuarioRepository;
        }

        public async Task<LoginResponse?> IniciarSesionAsync(
            string usuario,
            string password)
        {
            // ==========================================
            // BUSCAR USUARIO
            // ==========================================

            var usuarioBD =
                await _usuarioRepository.ObtenerPorUsuarioAsync(usuario);

            if (usuarioBD == null)
            {
                return null;
            }

            // ==========================================
            // VERIFICAR QUE ESTÉ ACTIVO
            // ==========================================

            if (!usuarioBD.Activo)
            {
                return null;
            }

            // ==========================================
            // VERIFICAR CONTRASEÑA
            // ==========================================
            //
            // IMPORTANTE:
            // Aquí posteriormente se verificará la contraseña
            // contra PasswordHash almacenado en la BD.
            //
            // No estamos comparando contraseñas directamente.
            // ==========================================

            bool passwordCorrecta = BCrypt.Net.BCrypt.Verify(
                password,
                usuarioBD.PasswordHash
            );

            if (!passwordCorrecta)
            {
                return null;
            }

            // ==========================================
            // GENERAR JWT
            // ==========================================

            var token = GenerarToken(
                usuarioBD.UsuarioNombre,
                usuarioBD.NombreCompleto,
                usuarioBD.Rol
            );

            return new LoginResponse
            {
                Token = token,

                Usuario = new UsuarioSesion
                {
                    Usuario = usuarioBD.UsuarioNombre,
                    Nombre = usuarioBD.NombreCompleto,
                    Rol = usuarioBD.Rol
                }
            };
        }

        private string GenerarToken(
            string usuario,
            string nombre,
            string rol)
        {
            var claims = new[]
            {
                new Claim(
                    ClaimTypes.Name,
                    usuario
                ),

                new Claim(
                    ClaimTypes.GivenName,
                    nombre
                ),

                new Claim(
                    ClaimTypes.Role,
                    rol
                )
            };

            var key = _configuration["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidOperationException(
                    "No se encontró la clave Jwt:Key."
                );
            }

            var securityKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(key)
                );

            var credentials =
                new SigningCredentials(
                    securityKey,
                    SecurityAlgorithms.HmacSha256
                );

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }
    }
}