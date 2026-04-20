using System.Text.Json.Serialization;

namespace SICRY_APP.Models
{
	public class UsuarioPerfil
	{
		[JsonPropertyName("idUsuario")]
		public int IdUsuario { get; set; }

		[JsonPropertyName("nombre")]
		public string Nombre { get; set; }

		[JsonPropertyName("apellido")]
		public string Apellido { get; set; }

		[JsonPropertyName("username")]
		public string Username { get; set; }

		[JsonPropertyName("activo")]
		public bool Activo { get; set; }

		[JsonPropertyName("idRol")]
		public int IdRol { get; set; }

		[JsonPropertyName("rolNombre")]
		public string RolNombre { get; set; }

		[JsonPropertyName("nombreCompleto")]
		public string NombreCompleto { get; set; }
	}
}