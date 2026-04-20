using System.Text.Json.Serialization;

namespace SICRY_APP.Models
{
	public class CategoriaFallo
	{
		[JsonPropertyName("idCategoriaFallo")]
		public int IdCategoriaFallo { get; set; }

		[JsonPropertyName("categoriaFallo")]
		public string CategoriaFalloNombre { get; set; }

		[JsonPropertyName("descripcionCategoriaFallo")]
		public string Descripcion { get; set; }

		public override string ToString() => CategoriaFalloNombre;
	}

	public class Refaccion
	{
		[JsonPropertyName("idRefaccion")]
		public int IdRefaccion { get; set; }

		[JsonPropertyName("nombreRefaccion")]
		public string NombreRefaccion { get; set; }

		[JsonPropertyName("codigoPieza")]
		public string CodigoPieza { get; set; }

		public override string ToString() => $"{NombreRefaccion} ({CodigoPieza})";
	}

	public class Motor
	{
		[JsonPropertyName("idMotor")]
		public int IdMotor { get; set; }

		[JsonPropertyName("numeroSerie")]
		public string NumeroSerie { get; set; }

		[JsonPropertyName("modelo")]
		public string Modelo { get; set; }

		public override string ToString() => $"{NumeroSerie} - {Modelo}";
	}

	// Item de lista dinámica: refacción + cantidad
	public class RefaccionSeleccionada
	{
		public Refaccion Refaccion { get; set; }
		public int Cantidad { get; set; } = 1;
		public string DisplayText => $"{Refaccion?.NombreRefaccion} x{Cantidad}";
	}

	// Respuesta del endpoint /api/evidenciasupload/sas
	public class SasResponse
	{
		[JsonPropertyName("uploadUrl")]
		public string UploadUrl { get; set; }

		[JsonPropertyName("publicUrl")]
		public string PublicUrl { get; set; }

		[JsonPropertyName("blobName")]
		public string BlobName { get; set; }
	}
}
