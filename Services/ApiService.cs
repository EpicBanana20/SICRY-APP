using Microsoft.Maui.Storage;
using SICRY_APP.Models;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SICRY_APP.Services
{
    public class ApiService
    {
        private static ApiService _instance;
        public static ApiService Instance => _instance ??= new ApiService();

        private readonly string _baseUrl = "https://sicryapi-cefncwd9fph6ecbt.mexicocentral-01.azurewebsites.net/api";
        private readonly HttpClient _httpClient;
        private const string TokenKey = "jwt_token";

        // Caches para evitar múltiples llamadas al cargar la lista
        private List<UsuarioMini> _usuariosCache;
        private List<PozoMini> _pozosCache;

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

                string rolNombre = idRol switch
                {
                    "1" => "Electricista",
                    "4" => "Mecanico",
                    "5" => "Supervisor",
                    "6" => "Embobinador",
                    _ => "Desconocido"
                };

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

        // Obtiene el ID del usuario logueado desde el JWT
        public async Task<int> GetIdUsuarioAsync()
        {
            var perfil = await GetPerfilDesdeTokenAsync();
            return perfil?.IdUsuario ?? 0;
        }

        // ======================= ASIGNACIONES =======================

        public async Task<List<Asignacion>> GetMisAsignacionesAsync()
        {
            try
            {
                var token = await GetTokenAsync();
                if (string.IsNullOrEmpty(token)) return new List<Asignacion>();

                int idUsuario = await GetIdUsuarioAsync();
                if (idUsuario == 0) return new List<Asignacion>();

                // Traer todas las asignaciones (la API aún no filtra por usuario)
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/asignaciones");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"========== ASIGNACIONES: {content} ==========");

                if (!response.IsSuccessStatusCode) return new List<Asignacion>();

                var todas = await response.Content.ReadFromJsonAsync<List<Asignacion>>();
                if (todas == null) return new List<Asignacion>();

                // Filtrar solo las que son para el usuario logueado
                var mias = todas.Where(a => a.IdUsuarioEmpleado == idUsuario).ToList();

                // Enriquecer con nombre del supervisor y ubicación del pozo
                await EnriquecerAsignacionesAsync(mias);

                return mias;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error GetMisAsignaciones: {ex.Message}");
                return new List<Asignacion>();
            }
        }

        private async Task EnriquecerAsignacionesAsync(List<Asignacion> asignaciones)
        {
            if (!asignaciones.Any()) return;

            var usuarios = await GetUsuariosAsync();
            var pozos = await GetPozosAsync();

            foreach (var a in asignaciones)
            {
                var sup = usuarios.FirstOrDefault(u => u.Id == a.IdUsuarioSupervisor);
                if (sup != null)
                {
                    var nombre = $"{sup.FirstName} {sup.LastName}".Trim();
                    a.AsignadoPor = string.IsNullOrWhiteSpace(nombre) ? sup.UserName : nombre;
                }

                if (a.IdPozo.HasValue)
                {
                    var pozo = pozos.FirstOrDefault(p => p.IdPozo == a.IdPozo.Value);
                    if (pozo != null)
                        a.Pozo = pozo.UbicacionPozo ?? "Sin pozo";
                }
            }
        }

        private async Task<List<UsuarioMini>> GetUsuariosAsync()
        {
            if (_usuariosCache != null) return _usuariosCache;

            try
            {
                var token = await GetTokenAsync();
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/usuarios");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    _usuariosCache = new List<UsuarioMini>();
                    return _usuariosCache;
                }

                _usuariosCache = await response.Content.ReadFromJsonAsync<List<UsuarioMini>>() ?? new List<UsuarioMini>();
                return _usuariosCache;
            }
            catch
            {
                _usuariosCache = new List<UsuarioMini>();
                return _usuariosCache;
            }
        }

        private async Task<List<PozoMini>> GetPozosAsync()
        {
            if (_pozosCache != null) return _pozosCache;

            try
            {
                var token = await GetTokenAsync();
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/pozos");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    _pozosCache = new List<PozoMini>();
                    return _pozosCache;
                }

                _pozosCache = await response.Content.ReadFromJsonAsync<List<PozoMini>>() ?? new List<PozoMini>();
                return _pozosCache;
            }
            catch
            {
                _pozosCache = new List<PozoMini>();
                return _pozosCache;
            }
        }

        public async Task<string> GetTokenAsync()
        {
            return await SecureStorage.Default.GetAsync(TokenKey);
        }

        public void Logout()
        {
            SecureStorage.Default.Remove(TokenKey);
            _usuariosCache = null;
            _pozosCache = null;
        }
    }

    public class LoginResponse
    {
        public string Token { get; set; }
    }
}