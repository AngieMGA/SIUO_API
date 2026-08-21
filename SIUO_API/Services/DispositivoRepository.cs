using Microsoft.Data.SqlClient;
using SIUO_API.Models;

namespace SIUO_API.Services
{
    public class DispositivoRepository : IDispositivoRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;

        public DispositivoRepository(
            SqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<List<Dispositivo>> ObtenerTodosAsync()
        {
            var dispositivos = new List<Dispositivo>();

            using var connection =
                _connectionFactory.CreateConnection();

            await connection.OpenAsync();

            const string sql = @"
                SELECT
                    IdDispositivo,
                    IdentificadorDispositivo,
                    NombreDispositivo,
                    TipoDispositivo,
                    Activo,
                    FechaRegistro,
                    FechaActualizacion
                FROM Dispositivos
                ORDER BY IdDispositivo;
            ";

            using var command =
                new SqlCommand(sql, connection);

            using var reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                dispositivos.Add(MapearDispositivo(reader));
            }

            return dispositivos;
        }

        public async Task<Dispositivo?> ObtenerPorIdentificadorAsync(
            string identificador)
        {
            using var connection =
                _connectionFactory.CreateConnection();

            await connection.OpenAsync();

            const string sql = @"
                SELECT
                    IdDispositivo,
                    IdentificadorDispositivo,
                    NombreDispositivo,
                    TipoDispositivo,
                    Activo,
                    FechaRegistro,
                    FechaActualizacion
                FROM Dispositivos
                WHERE IdentificadorDispositivo = @Identificador
                  AND Activo = 1;
            ";

            using var command =
                new SqlCommand(sql, connection);

            command.Parameters.AddWithValue(
                "@Identificador",
                identificador
            );

            using var reader =
                await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return MapearDispositivo(reader);
            }

            return null;
        }

        public async Task<Dispositivo?> ObtenerPorIdAsync(int id)
        {
            using var connection =
                _connectionFactory.CreateConnection();

            await connection.OpenAsync();

            const string sql = @"
                SELECT
                    IdDispositivo,
                    IdentificadorDispositivo,
                    NombreDispositivo,
                    TipoDispositivo,
                    Activo,
                    FechaRegistro,
                    FechaActualizacion
                FROM Dispositivos
                WHERE IdDispositivo = @Id;
            ";

            using var command =
                new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Id", id);

            using var reader =
                await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return MapearDispositivo(reader);
            }

            return null;
        }

        public async Task<bool> ActualizarIdentificadorAsync(
            int id,
            string identificador)
        {
            using var connection =
                _connectionFactory.CreateConnection();

            await connection.OpenAsync();

            const string sql = @"
                UPDATE Dispositivos
                SET
                    IdentificadorDispositivo = @Identificador,
                    FechaActualizacion = GETDATE()
                WHERE IdDispositivo = @Id;
            ";

            using var command =
                new SqlCommand(sql, connection);

            command.Parameters.AddWithValue(
                "@Identificador",
                identificador
            );

            command.Parameters.AddWithValue(
                "@Id",
                id
            );

            var filasAfectadas =
                await command.ExecuteNonQueryAsync();

            return filasAfectadas > 0;
        }

        private static Dispositivo MapearDispositivo(
            SqlDataReader reader)
        {
            return new Dispositivo
            {
                IdDispositivo =
                    reader.GetInt32(
                        reader.GetOrdinal("IdDispositivo")
                    ),

                IdentificadorDispositivo =
                    reader.IsDBNull(
                        reader.GetOrdinal(
                            "IdentificadorDispositivo"
                        )
                    )
                        ? null
                        : reader.GetString(
                            reader.GetOrdinal(
                                "IdentificadorDispositivo"
                            )
                        ),

                NombreDispositivo =
                    reader.GetString(
                        reader.GetOrdinal(
                            "NombreDispositivo"
                        )
                    ),

                TipoDispositivo =
                    reader.IsDBNull(
                        reader.GetOrdinal(
                            "TipoDispositivo"
                        )
                    )
                        ? null
                        : reader.GetString(
                            reader.GetOrdinal(
                                "TipoDispositivo"
                            )
                        ),

                Activo =
                    reader.GetBoolean(
                        reader.GetOrdinal("Activo")
                    ),

                FechaRegistro =
                    reader.GetDateTime(
                        reader.GetOrdinal("FechaRegistro")
                    ),

                FechaActualizacion =
                    reader.IsDBNull(
                        reader.GetOrdinal(
                            "FechaActualizacion"
                        )
                    )
                        ? null
                        : reader.GetDateTime(
                            reader.GetOrdinal(
                                "FechaActualizacion"
                            )
                        )
            };
        }
    }
}