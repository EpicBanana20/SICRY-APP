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

        [JsonPropertyName("repTieneHorasExtras")]
        public bool? RepTieneHorasExtras { get; set; }

        [JsonPropertyName("repHorasExtras")]
        public decimal? RepHorasExtras { get; set; }
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

        [JsonPropertyName("repEmbTieneHorasExtras")]
        public bool? RepEmbTieneHorasExtras { get; set; }

        [JsonPropertyName("repEmbHorasExtras")]
        public decimal? RepEmbHorasExtras { get; set; }
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

        [JsonPropertyName("repManTieneHorasExtras")]
        public bool? RepManTieneHorasExtras { get; set; }

        [JsonPropertyName("repManHorasExtras")]
        public decimal? RepManHorasExtras { get; set; }
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
        public bool TieneHorasExtras { get; set; }
        public int HorasExtras { get; set; }

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

    public class FalloDeReporte
    {
        [JsonPropertyName("idFalloReportado")]
        public int IdFalloReportado { get; set; }

        [JsonPropertyName("idCategoriaFalloFk")]
        public int IdCategoriaFalloFk { get; set; }

        [JsonPropertyName("nombreCategoriaFallo")]
        public string NombreCategoriaFallo { get; set; } = string.Empty;
    }

    public class RefaccionDeReporte
    {
        [JsonPropertyName("idRefaccionesUsadas")]
        public int IdRefaccionesUsadas { get; set; }

        [JsonPropertyName("nombreRefaccion")]
        public string NombreRefaccion { get; set; } = string.Empty;

        [JsonPropertyName("codigoPieza")]
        public string? CodigoPieza { get; set; }

        [JsonPropertyName("cantidad")]
        public int Cantidad { get; set; }

        public string DisplayText => $"{NombreRefaccion} x{Cantidad}";
    }

    public class EvidenciaDeReporte
    {
        [JsonPropertyName("idEvidencias")]
        public int IdEvidencias { get; set; }

        [JsonPropertyName("evUrlArchivo")]
        public string EvUrlArchivo { get; set; } = string.Empty;

        [JsonPropertyName("evTipoEvidencia")]
        public string EvTipoEvidencia { get; set; } = string.Empty;
    }
}
