using System.Net.Http.Json;

namespace SICRY_APP.Services
{
    public class ApiService
    {
        // Pega aquí tu IP de Windows en lugar de las X. Respeta el puerto 5128.
        private readonly string _baseUrl = "http://192.168.1.84:5128/api";
        private readonly HttpClient _httpClient;

        public ApiService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<string> LoginAsync(string username, string password)
        {
            try
            {
                // Empaquetamos los datos como lo espera la API
                var loginData = new { userName = username, password = password };

                // Hacemos la llamada POST a Auth/login
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/Auth/login", loginData);

                if (response.IsSuccessStatusCode)
                {
                    // Si el login es correcto, extraemos el Token JWT
                    var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                    return result?.Token;
                }

                return null; // Credenciales inválidas
            }
            catch (Exception ex)
            {
                // Error de conexión (ej. Servidor apagado o IP incorrecta)
                return null;
            }
        }
    }

    // Clase auxiliar para leer la respuesta JSON de tu API
    public class LoginResponse
    {
        public string Token { get; set; }
    }
}