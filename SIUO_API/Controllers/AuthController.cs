using Microsoft.AspNetCore.Mvc;
using SIUO_API.DTOs;
using SIUO_API.Services;

namespace SIUO_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Usuario) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    mensaje = "Ingrese usuario y contraseña."
                });
            }

            var resultado =
                await _authService.IniciarSesionAsync(
                    request.Usuario,
                    request.Password
                );

            if (resultado == null)
            {
                return Unauthorized(new
                {
                    mensaje = "Usuario o contraseña incorrectos."
                });
            }

            return Ok(resultado);
        }
    }
}