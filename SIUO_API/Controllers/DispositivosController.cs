using Microsoft.AspNetCore.Mvc;
using SIUO_API.Services;

namespace SIUO_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DispositivosController : ControllerBase
    {
        private readonly IDispositivoRepository _repository;

        public DispositivosController(
            IDispositivoRepository repository)
        {
            _repository = repository;
        }

        // ==========================================
        // OBTENER TODOS LOS DISPOSITIVOS
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> ObtenerTodos()
        {
            var dispositivos =
                await _repository.ObtenerTodosAsync();

            return Ok(dispositivos);
        }

        // ==========================================
        // OBTENER DISPOSITIVO POR ID
        // ==========================================

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var dispositivo =
                await _repository.ObtenerPorIdAsync(id);

            if (dispositivo == null)
            {
                return NotFound(
                    "No se encontró el dispositivo."
                );
            }

            return Ok(dispositivo);
        }

        // ==========================================
        // OBTENER POR IDENTIFICADOR
        // ==========================================

        [HttpGet("identificador/{identificador}")]
        public async Task<IActionResult> ObtenerPorIdentificador(
            string identificador)
        {
            var dispositivo =
                await _repository
                    .ObtenerPorIdentificadorAsync(
                        identificador
                    );

            if (dispositivo == null)
            {
                return NotFound(
                    "No se encontró un dispositivo con ese identificador."
                );
            }

            return Ok(dispositivo);
        }

        // ==========================================
        // ASIGNAR / ACTUALIZAR IDENTIFICADOR
        // ==========================================

        [HttpPut("{id:int}/identificador")]
        public async Task<IActionResult> ActualizarIdentificador(
            int id,
            [FromBody] string identificador)
        {
            if (string.IsNullOrWhiteSpace(identificador))
            {
                return BadRequest(
                    "El identificador es obligatorio."
                );
            }

            var dispositivo =
                await _repository.ObtenerPorIdAsync(id);

            if (dispositivo == null)
            {
                return NotFound(
                    "No se encontró el dispositivo."
                );
            }

            var actualizado =
                await _repository.ActualizarIdentificadorAsync(
                    id,
                    identificador
                );

            if (!actualizado)
            {
                return BadRequest(
                    "No se pudo actualizar el identificador."
                );
            }

            return Ok(
                new
                {
                    mensaje = "Identificador actualizado correctamente.",
                    idDispositivo = id,
                    identificador = identificador
                }
            );
        }
    }
}