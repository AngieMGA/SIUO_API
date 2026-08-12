using Microsoft.Data.SqlClient;

namespace SIUO_API.Services
{
    public class SqlConnectionFactory
    {
        private readonly IConfiguration _configuration;

        public SqlConnectionFactory(
            IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public SqlConnection CreateConnection()
        {
            var connectionString =
                _configuration.GetConnectionString(
                    "DefaultConnection"
                );

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "No se encontró la cadena de conexión DefaultConnection."
                );
            }

            return new SqlConnection(connectionString);
        }
    }
}