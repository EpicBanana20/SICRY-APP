using System.Text.Json.Serialization;

namespace SICRY_APP.Models
{
    public class Reporte
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Estado { get; set; }
        public string Ubicacion { get; set; }
        public string Descripcion { get; set; }
        public string ColorEstadoFondo { get; set; }
        public string ColorEstadoTexto { get; set; }
    }

    public class ReporteElectricista
    {
        [JsonPropertyName("idReporteCampo")]
        public int IdReporteCampo { get; set; }

        [JsonPropertyName("idAsignacion")]
        public int IdAsignacion { get; set; }

        [JsonPropertyName("idPozo")]
        public int IdPozo { get; set; }

        [JsonPropertyName("repFechaReporte")]
        public DateTime RepFechaReporte { get; set; }

        [JsonPropertyName("repEsConclusivo")]
        public bool RepEsConclusivo { get; set; }

        [JsonPropertyName("repDescripcion")]
        public string RepDescripcion { get; set; } = string.Empty;
    }

    public class ReporteEmbobinado
    {
        [JsonPropertyName("idReporteEmbobinado")]
        public int IdReporteEmbobinado { get; set; }

        [JsonPropertyName("idAsignacion")]
        public int IdAsignacion { get; set; }

        [JsonPropertyName("idMotor")]
        public int IdMotor { get; set; }

        [JsonPropertyName("repEmbFechaReporte")]
        public DateTime RepEmbFechaReporte { get; set; }

        [JsonPropertyName("repEmbEsConclusivo")]
        public bool RepEmbEsConclusivo { get; set; }

        [JsonPropertyName("repEmbDescripcion")]
        public string RepEmbDescripcion { get; set; } = string.Empty;
    }

    public class ReporteMantenimiento
    {
        [JsonPropertyName("idReporteMantenimiento")]
        public int IdReporteMantenimiento { get; set; }

        [JsonPropertyName("idAsignacion")]
        public int IdAsignacion { get; set; }

        [JsonPropertyName("idMotor")]
        public int IdMotor { get; set; }

        [JsonPropertyName("repManFechaReporte")]
        public DateTime RepManFechaReporte { get; set; }

        [JsonPropertyName("repManEsConclusivo")]
        public bool RepManEsConclusivo { get; set; }

        [JsonPropertyName("repManDescripcion")]
        public string RepManDescripcion { get; set; } = string.Empty;
    }

    // DTO unificado para el listado y edición en la app
    public class ReporteItem
    {
        public int Id { get; set; }
        public int IdAsignacion { get; set; }
        public int IdPozo { get; set; }
        public int IdMotor { get; set; }
        public string Tipo { get; set; } = string.Empty; // electricista | embobinado | mantenimiento
        public DateTime Fecha { get; set; }
        public bool EsConclusivo { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;

        public string Titulo => Tipo switch
        {
            "electricista" => "Reporte Eléctrico",
            "embobinado" => "Reporte de Embobinado",
            "mantenimiento" => "Reporte de Mantenimiento",
            _ => "Reporte"
        };

        public string Estado => EsConclusivo ? "Conclusivo" : "Inconcluso";
        public string FechaTexto => Fecha.ToString("dd/MM/yyyy");

        public string ColorEstadoFondo => EsConclusivo ? "#E8F5E9" : "#FFF3E0";
        public string ColorEstadoTexto => EsConclusivo ? "#2E7D32" : "#E65100";
    }
}
