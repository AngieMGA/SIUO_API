using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;

namespace SIUO_API.Services
{
    public class ChecklistConnectionFactory
    {
        private readonly IConfiguration _configuration;

        public ChecklistConnectionFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public SqlConnection CreateConnection()
        {
            var connectionString =
                _configuration.GetConnectionString("ChecklistConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "No se encontró la cadena de conexión ChecklistConnection."
                );
            }

            return new SqlConnection(connectionString);
        }
    }
}