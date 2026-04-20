using System.Text.Json.Serialization;

namespace SICRY_APP.Models
{
    public class Asignacion
    {
        [JsonPropertyName("idAsignacion")]
        public int IdAsignacion { get; set; }

        [JsonPropertyName("idUsuarioSupervisor")]
        public int IdUsuarioSupervisor { get; set; }

        [JsonPropertyName("idUsuarioEmpleado")]
        public int IdUsuarioEmpleado { get; set; }

        [JsonPropertyName("idPozo")]
        public int? IdPozo { get; set; }

        [JsonPropertyName("fechaCreacionAsignacion")]
        public DateTime FechaCreacionAsignacion { get; set; }

        [JsonPropertyName("estado")]
        public string Estado { get; set; }

        [JsonPropertyName("instruccionInicial")]
        public string InstruccionInicial { get; set; }

        // Campos que se llenan en la app (la API actual no los trae)
        public string AsignadoPor { get; set; } = "Supervisor";
        public string Pozo { get; set; } = "Sin pozo";

        [JsonIgnore]
        public string FechaFormateada => FechaCreacionAsignacion.ToString("dd/MM/yyyy");

        [JsonIgnore]
        public string ColorEstado => Estado?.ToLower() switch
        {
            "completada" or "completado" => "#2E7D32",
            "en progreso" => "#1565C0",
            "pendiente" or "por hacer" => "#E65100",
            "inconclusa" => "#C62828",
            _ => "#555555"
        };

        [JsonIgnore]
        public string ColorEstadoFondo => Estado?.ToLower() switch
        {
            "completada" or "completado" => "#E8F5E9",
            "en progreso" => "#E3F2FD",
            "pendiente" or "por hacer" => "#FFF3E0",
            "inconclusa" => "#FFEBEE",
            _ => "#EEEEEE"
        };
    }

    // Modelos auxiliares para traer nombre del supervisor y pozo
    public class UsuarioMini
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("firstName")]
        public string FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string LastName { get; set; }

        [JsonPropertyName("userName")]
        public string UserName { get; set; }
    }

    public class PozoMini
    {
        [JsonPropertyName("idPozo")]
        public int IdPozo { get; set; }

        [JsonPropertyName("ubicacionPozo")]
        public string UbicacionPozo { get; set; }
    }
}