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

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT
                    DB_NAME() AS BaseDatos,
                    SUSER_SNAME() AS UsuarioSQL,
                    OBJECT_ID('userchecklist.INSPECCION', 'U') AS TablaInspeccion,
                    OBJECT_ID('userchecklist.FOLIO_CONSECUTIVO', 'U') AS TablaFolios;
            ";

            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();

            return Ok(new
            {
                baseDatos = reader["BaseDatos"].ToString(),
                usuarioSQL = reader["UsuarioSQL"].ToString(),
                tablaInspeccion =
                    reader["TablaInspeccion"] == DBNull.Value
                        ? null
                        : reader["TablaInspeccion"].ToString(),
                tablaFolios =
                    reader["TablaFolios"] == DBNull.Value
                        ? null
                        : reader["TablaFolios"].ToString()
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                mensaje = "Falló la prueba de conexión.",
                error = ex.Message
            });
        }
    }
    }
}