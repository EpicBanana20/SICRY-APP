using Microsoft.Maui.Storage;
using SICRY_APP.Models;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SICRY_APP.Services
{
	public class ApiService
	{
		// Singleton: una sola instancia compartida
		private static ApiService _instance;
		public static ApiService Instance => _instance ??= new ApiService();

		private readonly string _baseUrl = "https://sicryapi-cefncwd9fph6ecbt.mexicocentral-01.azurewebsites.net/api";
		private readonly HttpClient _httpClient;
		private const string TokenKey = "jwt_token";

		private ApiService()
		{
			_httpClient = new HttpClient();
		}

		public async Task<bool> LoginAsync(string username, string password)
		{
			try
			{
				var loginData = new { userName = username, password = password };
				var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/Auth/login", loginData);

				if (!response.IsSuccessStatusCode) return false;

				var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
				if (string.IsNullOrEmpty(result?.Token)) return false;

				await SecureStorage.Default.SetAsync(TokenKey, result.Token);
				return true;
			}
			catch
			{
				return false;
			}
		}

		public async Task<UsuarioPerfil> GetPerfilUsuarioAsync()
		{
			try
			{
				var token = await GetTokenAsync();
				if (string.IsNullOrEmpty(token)) return null;

				var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/usuarios/me");
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

				var response = await _httpClient.SendAsync(request);
				if (!response.IsSuccessStatusCode) return null;

				return await response.Content.ReadFromJsonAsync<UsuarioPerfil>();
			}
			catch
			{
				return null;
			}
		}

		// Decodifica el JWT y extrae los claims como perfil
		public async Task<UsuarioPerfil> GetPerfilDesdeTokenAsync()
		{
			try
			{
				var token = await GetTokenAsync();
				if (string.IsNullOrEmpty(token)) return null;

				var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
				var jwt = handler.ReadJwtToken(token);

				string username = jwt.Claims.FirstOrDefault(c =>
					c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"
					|| c.Type == "name" || c.Type == "unique_name")?.Value;

				string idUsuario = jwt.Claims.FirstOrDefault(c =>
					c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
					|| c.Type == "nameid")?.Value;

				string idRol = jwt.Claims.FirstOrDefault(c =>
					c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
					|| c.Type == "role")?.Value;

				// Mapeo provisional de IDs → nombres de rol
				string rolNombre = idRol switch
				{
					"1" => "Electricista",
					"4" => "Mecanico",
					"5" => "Supervisor",
					"6" => "Embobinador",
					_ => "Desconocido"
				};

				// Capitalizar el username
				string nombreFormateado = !string.IsNullOrEmpty(username)
					? char.ToUpper(username[0]) + username.Substring(1)
					: "Usuario";

				return new UsuarioPerfil
				{
					IdUsuario = int.TryParse(idUsuario, out int id) ? id : 0,
					Nombre = nombreFormateado,
					Apellido = "",
					Username = username ?? "",
					IdRol = int.TryParse(idRol, out int r) ? r : 0,
					RolNombre = rolNombre,
					NombreCompleto = nombreFormateado
				};
			}
			catch
			{
				return null;
			}
		}

		public async Task<string> GetTokenAsync()
		{
			return await SecureStorage.Default.GetAsync(TokenKey);
		}

		public void Logout()
		{
			SecureStorage.Default.Remove(TokenKey);
		}
	}

	public class LoginResponse
	{
		public string Token { get; set; }
	}
}