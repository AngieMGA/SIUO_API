namespace SIUO_API.Models
{
    public class Dispositivo
    {
        public int IdDispositivo { get; set; }

        public string? IdentificadorDispositivo { get; set; }

        public string NombreDispositivo { get; set; } = string.Empty;

        public string? TipoDispositivo { get; set; }

        public bool Activo { get; set; }

        public DateTime FechaRegistro { get; set; }

        public DateTime? FechaActualizacion { get; set; }
    }
}