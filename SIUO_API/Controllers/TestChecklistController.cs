using Microsoft.AspNetCore.Mvc;
using SIUO_API.Services;

namespace SIUO_API.Controllers
{
    [ApiController]
    [Route("api/test-checklist")]
    public class TestChecklistController : ControllerBase
    {
        private readonly ChecklistConnectionFactory _connectionFactory;

        public TestChecklistController(
            ChecklistConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        [HttpGet("conexion")]
        public async Task<IActionResult> ProbarConexion()
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();

                await connection.OpenAsync();

                return Ok(new
                {
                    mensaje = "Conexión exitosa con la base de datos Checklist."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "No se pudo conectar con la base de datos Checklist.",
                    error = ex.Message
                });
            }
        }
    }
}