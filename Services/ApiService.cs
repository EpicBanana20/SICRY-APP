using Microsoft.Maui.Storage;
using SICRY_APP.Models;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IO;
using Microsoft.Maui.Storage;

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
        private List<CategoriaFallo> _categoriasCache;
        private List<Refaccion> _refaccionesCache;
        private List<Motor> _motoresCache;

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

        // ============ CATÁLOGOS ============

        public async Task<List<CategoriaFallo>> GetCategoriasFallosAsync()
        {
            if (_categoriasCache != null) return _categoriasCache;
            try
            {
                var token = await GetTokenAsync();
                var req = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/categoriafallos");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var resp = await _httpClient.SendAsync(req);
                if (!resp.IsSuccessStatusCode) { _categoriasCache = new(); return _categoriasCache; }
                _categoriasCache = await resp.Content.ReadFromJsonAsync<List<CategoriaFallo>>() ?? new();
                return _categoriasCache;
            }
            catch { _categoriasCache = new(); return _categoriasCache; }
        }

        public async Task<List<Refaccion>> GetRefaccionesAsync()
        {
            if (_refaccionesCache != null) return _refaccionesCache;
            try
            {
                var token = await GetTokenAsync();
                var req = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/refacciones");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var resp = await _httpClient.SendAsync(req);
                if (!resp.IsSuccessStatusCode) { _refaccionesCache = new(); return _refaccionesCache; }
                _refaccionesCache = await resp.Content.ReadFromJsonAsync<List<Refaccion>>() ?? new();
                return _refaccionesCache;
            }
            catch { _refaccionesCache = new(); return _refaccionesCache; }
        }

        public async Task<List<Motor>> GetMotoresAsync()
        {
            if (_motoresCache != null) return _motoresCache;
            try
            {
                var token = await GetTokenAsync();
                var req = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/motores");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var resp = await _httpClient.SendAsync(req);
                if (!resp.IsSuccessStatusCode) { _motoresCache = new(); return _motoresCache; }
                _motoresCache = await resp.Content.ReadFromJsonAsync<List<Motor>>() ?? new();
                return _motoresCache;
            }
            catch { _motoresCache = new(); return _motoresCache; }
        }

        // ============ CREAR REPORTE (según rol) ============
        // Devuelve el ID del reporte creado, o 0 si falló

        public async Task<int> CrearReporteElectricistaAsync(int idAsignacion, int idPozo, bool esConclusivo, string descripcion,
    bool tieneHorasExtras, int horasExtras)
        {
            try
            {
                var token = await GetTokenAsync();
                var body = new
                {
                    idAsignacion,
                    idPozo,
                    repFechaReporte = DateTime.UtcNow,
                    repEsConclusivo = esConclusivo,
                    repDescripcion = descripcion,
                    repTieneHorasExtras = tieneHorasExtras,
                    repHorasExtras = tieneHorasExtras ? (decimal?)horasExtras : null
                };
                var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/repelectricista")
                { Content = JsonContent.Create(body) };
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var resp = await _httpClient.SendAsync(req);
                var contenido = await resp.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"===== RepElectricista status={resp.StatusCode} body={contenido} =====");
                if (!resp.IsSuccessStatusCode) return 0;
                var result = await resp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                return result.TryGetProperty("idReporteCampo", out var idProp) ? idProp.GetInt32() : 0;
            }
            catch { return 0; }
        }

        public async Task<int> CrearReporteEmbobinadoAsync(int idAsignacion, int idMotor, bool esConclusivo, string descripcion,
    bool tieneHorasExtras, int horasExtras)
        {
            try
            {
                var token = await GetTokenAsync();
                var body = new
                {
                    idAsignacion,
                    idMotor,
                    repEmbFechaReporte = DateTime.UtcNow,
                    repEmbEsConclusivo = esConclusivo,
                    repEmbDescripcion = descripcion,
                    repEmbTieneHorasExtras = tieneHorasExtras,
                    repEmbHorasExtras = tieneHorasExtras ? (decimal?)horasExtras : null
                };
                var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/repembobinado")
                { Content = JsonContent.Create(body) };
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var resp = await _httpClient.SendAsync(req);
                if (!resp.IsSuccessStatusCode) return 0;
                var result = await resp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                return result.TryGetProperty("idReporteEmbobinado", out var idProp) ? idProp.GetInt32() : 0;
            }
            catch { return 0; }
        }

        public async Task<int> CrearReporteMantenimientoAsync(int idAsignacion, int idMotor, bool esConclusivo, string descripcion,
    bool tieneHorasExtras, int horasExtras)
        {
            try
            {
                var token = await GetTokenAsync();
                var body = new
                {
                    idAsignacion,
                    idMotor,
                    repManFechaReporte = DateTime.UtcNow,
                    repManEsConclusivo = esConclusivo,
                    repManDescripcion = descripcion,
                    repManTieneHorasExtras = tieneHorasExtras,
                    repManHorasExtras = tieneHorasExtras ? (decimal?)horasExtras : null
                };
                var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/repmantenimiento")
                { Content = JsonContent.Create(body) };
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var resp = await _httpClient.SendAsync(req);
                if (!resp.IsSuccessStatusCode) return 0;
                var result = await resp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                return result.TryGetProperty("idReporteMantenimiento", out var idProp) ? idProp.GetInt32() : 0;
            }
            catch { return 0; }
        }

        // ============ FALLOS Y REFACCIONES USADAS ============

        public async Task<bool> AgregarFalloReportadoAsync(int idCategoriaFallo, string tipoReporte, int idReporte)
        {
            try
            {
                var token = await GetTokenAsync();
                var body = new Dictionary<string, object>
                {
                    ["idCategoriaFalloFk"] = idCategoriaFallo
                };
                switch (tipoReporte)
                {
                    case "electricista": body["idReporteCampoFk"] = idReporte; break;
                    case "embobinado": body["idReporteEmbobinadoFk"] = idReporte; break;
                    case "mantenimiento": body["idReporteMantenimientoFk"] = idReporte; break;
                }
                var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/fallosreportados")
                { Content = JsonContent.Create(body) };
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var resp = await _httpClient.SendAsync(req);
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> AgregarRefaccionUsadaAsync(int idRefaccion, int cantidad, string tipoReporte, int idReporte)
        {
            try
            {
                var token = await GetTokenAsync();
                var body = new Dictionary<string, object>
                {
                    ["idRefaccion"] = idRefaccion,
                    ["cantidad"] = cantidad
                };
                switch (tipoReporte)
                {
                    case "electricista": body["idReporteCampo"] = idReporte; break;
                    case "embobinado": body["idReporteEmbobinado"] = idReporte; break;
                    case "mantenimiento": body["idReporteMantenimiento"] = idReporte; break;
                }
                var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/refaccionesusadas")
                { Content = JsonContent.Create(body) };
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var resp = await _httpClient.SendAsync(req);
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        // ============ EVIDENCIAS (BLOB + BD) ============

        public async Task<string> SubirEvidenciaAsync(Stream fotoStream, string extension, int idAsignacion, string tipoReporte, int idReporte)
        {
            try
            {
                var token = await GetTokenAsync();

                // 1. Pedir SAS URL
                var url = $"{_baseUrl}/evidencias/sas?idAsignacion={idAsignacion}&tipoReporte={tipoReporte}&idReporte={idReporte}&extension={Uri.EscapeDataString(extension)}";
                var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var resp = await _httpClient.SendAsync(req);
                if (!resp.IsSuccessStatusCode) return null;

                // Asumiendo que creaste una clase SasResponse con UploadUrl y PublicUrl
                var sas = await resp.Content.ReadFromJsonAsync<SasResponse>();
                if (sas == null) return null;

                // 2. Subir foto a Azure Blob (Reutilizando _httpClient)
                using var content = new StreamContent(fotoStream);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                    extension.ToLower() == ".png" ? "image/png" : "image/jpeg");

                var putReq = new HttpRequestMessage(HttpMethod.Put, sas.UploadUrl) { Content = content };
                putReq.Headers.Add("x-ms-blob-type", "BlockBlob");
                var uploadResp = await _httpClient.SendAsync(putReq);
                if (!uploadResp.IsSuccessStatusCode) return null;

                // 3. Registrar evidencia en BD
                var body = new Dictionary<string, object>
                {
                    ["evUrlArchivo"] = sas.PublicUrl,
                    ["evTipoEvidencia"] = extension.TrimStart('.').ToLower()
                };
                switch (tipoReporte)
                {
                    case "electricista": body["idReporteCampoFk"] = idReporte; break;
                    case "embobinado": body["idReporteEmbobinadoFk"] = idReporte; break;
                    case "mantenimiento": body["idReporteMantenimientoFk"] = idReporte; break;
                }
                var evReq = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/evidencias")
                { Content = JsonContent.Create(body) };
                evReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                await _httpClient.SendAsync(evReq);

                return sas.PublicUrl;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error SubirEvidencia: {ex.Message}");
                return null;
            }
        }

        // ============ ACTUALIZAR ESTADO DE ASIGNACIÓN ============

        public async Task<bool> CambiarEstadoAsignacionAsync(int idAsignacion, string nuevoEstado)
        {
            try
            {
                var token = await GetTokenAsync();
                var body = new { estado = nuevoEstado };
                var req = new HttpRequestMessage(HttpMethod.Patch, $"{_baseUrl}/asignaciones/{idAsignacion}/estado")
                { Content = JsonContent.Create(body) };
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var resp = await _httpClient.SendAsync(req);
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
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
            _categoriasCache = null;
            _refaccionesCache = null;
            _motoresCache = null;
        }

        // ============ LISTAR REPORTES DEL USUARIO ============

        public async Task<List<ReporteItem>> GetMisReportesAsync()
        {
            var resultado = new List<ReporteItem>();
            try
            {
                var token = await GetTokenAsync();
                if (string.IsNullOrEmpty(token)) return resultado;

                int idUsuario = await GetIdUsuarioAsync();
                if (idUsuario == 0) return resultado;

                // 1. Todas las asignaciones del usuario (para saber cuáles reportes son míos)
                var asignacionesReq = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/asignaciones");
                asignacionesReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var asignResp = await _httpClient.SendAsync(asignacionesReq);
                if (!asignResp.IsSuccessStatusCode) return resultado;
                var todas = await asignResp.Content.ReadFromJsonAsync<List<Asignacion>>() ?? new();
                var misAsignaciones = todas.Where(a => a.IdUsuarioEmpleado == idUsuario).ToList();
                var idsAsignaciones = misAsignaciones.Select(a => a.IdAsignacion).ToHashSet();
                if (idsAsignaciones.Count == 0) return resultado;

                var pozos = await GetPozosAsync();

                // 2. Electricista
                var reqE = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/repelectricista");
                reqE.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var respE = await _httpClient.SendAsync(reqE);
                if (respE.IsSuccessStatusCode)
                {
                    var lista = await respE.Content.ReadFromJsonAsync<List<ReporteElectricista>>() ?? new();
                    foreach (var r in lista.Where(x => idsAsignaciones.Contains(x.IdAsignacion)))
                    {
                        var pozo = pozos.FirstOrDefault(p => p.IdPozo == r.IdPozo);
                        resultado.Add(new ReporteItem
                        {
                            Id = r.IdReporteCampo,
                            IdAsignacion = r.IdAsignacion,
                            IdPozo = r.IdPozo,
                            Tipo = "electricista",
                            Fecha = r.RepFechaReporte,
                            EsConclusivo = r.RepEsConclusivo,
                            Descripcion = r.RepDescripcion,
                            Ubicacion = pozo?.UbicacionPozo ?? "Sin pozo",
                            TieneHorasExtras = r.RepTieneHorasExtras ?? false,
                            HorasExtras = (int)(r.RepHorasExtras ?? 0)
                        });
                    }
                }

                // 3. Embobinado
                var reqB = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/repembobinado");
                reqB.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var respB = await _httpClient.SendAsync(reqB);
                if (respB.IsSuccessStatusCode)
                {
                    var lista = await respB.Content.ReadFromJsonAsync<List<ReporteEmbobinado>>() ?? new();
                    foreach (var r in lista.Where(x => idsAsignaciones.Contains(x.IdAsignacion)))
                    {
                        resultado.Add(new ReporteItem
                        {
                            Id = r.IdReporteEmbobinado,
                            IdAsignacion = r.IdAsignacion,
                            IdMotor = r.IdMotor,
                            Tipo = "embobinado",
                            Fecha = r.RepEmbFechaReporte,
                            EsConclusivo = r.RepEmbEsConclusivo,
                            Descripcion = r.RepEmbDescripcion,
                            Ubicacion = $"Motor #{r.IdMotor}",
                            TieneHorasExtras = r.RepEmbTieneHorasExtras ?? false,
                            HorasExtras = (int)(r.RepEmbHorasExtras ?? 0)
                        });
                    }
                }

                // 4. Mantenimiento
                var reqM = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/repmantenimiento");
                reqM.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var respM = await _httpClient.SendAsync(reqM);
                if (respM.IsSuccessStatusCode)
                {
                    var lista = await respM.Content.ReadFromJsonAsync<List<ReporteMantenimiento>>() ?? new();
                    foreach (var r in lista.Where(x => idsAsignaciones.Contains(x.IdAsignacion)))
                    {
                        resultado.Add(new ReporteItem
                        {
                            Id = r.IdReporteMantenimiento,
                            IdAsignacion = r.IdAsignacion,
                            IdMotor = r.IdMotor,
                            Tipo = "mantenimiento",
                            Fecha = r.RepManFechaReporte,
                            EsConclusivo = r.RepManEsConclusivo,
                            Descripcion = r.RepManDescripcion,
                            Ubicacion = $"Motor #{r.IdMotor}",
                            TieneHorasExtras = r.RepManTieneHorasExtras ?? false,
                            HorasExtras = (int)(r.RepManHorasExtras ?? 0)
                        });
                    }
                }

                return resultado.OrderByDescending(r => r.Fecha).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error GetMisReportes: {ex.Message}");
                return resultado;
            }
        }

        // ============ ACTUALIZAR REPORTE ============

        public async Task<bool> ActualizarReporteAsync(ReporteItem item)
        {
            try
            {
                var token = await GetTokenAsync();
                if (string.IsNullOrEmpty(token)) return false;

                object body;
                string endpoint;
                switch (item.Tipo)
                {
                    case "electricista":
                        endpoint = $"{_baseUrl}/repelectricista/{item.Id}";
                        body = new
                        {
                            idReporteCampo = item.Id,
                            idAsignacion = item.IdAsignacion,
                            idPozo = item.IdPozo,
                            repFechaReporte = item.Fecha,
                            repEsConclusivo = item.EsConclusivo,
                            repDescripcion = item.Descripcion,
                            repTieneHorasExtras = item.TieneHorasExtras,
                            repHorasExtras = item.TieneHorasExtras ? item.HorasExtras : 0
                        };
                        break;
                    case "embobinado":
                        endpoint = $"{_baseUrl}/repembobinado/{item.Id}";
                        body = new
                        {
                            idReporteEmbobinado = item.Id,
                            idAsignacion = item.IdAsignacion,
                            idMotor = item.IdMotor,
                            repEmbFechaReporte = item.Fecha,
                            repEmbEsConclusivo = item.EsConclusivo,
                            repEmbDescripcion = item.Descripcion,
                            repEmbTieneHorasExtras = item.TieneHorasExtras,
                            repEmbHorasExtras = item.TieneHorasExtras ? item.HorasExtras : 0
                        };
                        break;
                    case "mantenimiento":
                        endpoint = $"{_baseUrl}/repmantenimiento/{item.Id}";
                        body = new
                        {
                            idReporteMantenimiento = item.Id,
                            idAsignacion = item.IdAsignacion,
                            idMotor = item.IdMotor,
                            repManFechaReporte = item.Fecha,
                            repManEsConclusivo = item.EsConclusivo,
                            repManDescripcion = item.Descripcion,
                            repManTieneHorasExtras = item.TieneHorasExtras,
                            repManHorasExtras = item.TieneHorasExtras ? item.HorasExtras : 0
                        };
                        break;
                    default: return false;
                }

                var req = new HttpRequestMessage(HttpMethod.Put, endpoint)
                { Content = JsonContent.Create(body) };
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var resp = await _httpClient.SendAsync(req);
                var contenido = await resp.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"===== PUT {item.Tipo} status={resp.StatusCode} body={contenido} =====");
                return resp.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error ActualizarReporte: {ex.Message}");
                return false;
            }
        }

        // ============ DETALLE DE REPORTE (fallos, refacciones, evidencias) ============

        public async Task<List<FalloDeReporte>> GetFallosDeReporteAsync(string tipo, int id)
        {
            try
            {
                var token = await GetTokenAsync();
                if (string.IsNullOrEmpty(token)) return new();
                string param = tipo switch
                {
                    "electricista" => $"reporteCampoId={id}",
                    "embobinado" => $"reporteEmbobinadoId={id}",
                    "mantenimiento" => $"reporteMantenimientoId={id}",
                    _ => null
                };
                if (param == null) return new();
                var req = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/fallosreportados?{param}");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var resp = await _httpClient.SendAsync(req);
                if (!resp.IsSuccessStatusCode) return new();
                return await resp.Content.ReadFromJsonAsync<List<FalloDeReporte>>() ?? new();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error GetFallos: {ex.Message}");
                return new();
            }
        }

        public async Task<List<RefaccionDeReporte>> GetRefaccionesDeReporteAsync(string tipo, int id)
        {
            try
            {
                var token = await GetTokenAsync();
                if (string.IsNullOrEmpty(token)) return new();
                string param = tipo switch
                {
                    "electricista" => $"reporteCampoId={id}",
                    "embobinado" => $"reporteEmbobinadoId={id}",
                    "mantenimiento" => $"reporteMantenimientoId={id}",
                    _ => null
                };
                if (param == null) return new();
                var req = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/refaccionesusadas?{param}");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var resp = await _httpClient.SendAsync(req);
                if (!resp.IsSuccessStatusCode) return new();
                return await resp.Content.ReadFromJsonAsync<List<RefaccionDeReporte>>() ?? new();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error GetRefacciones: {ex.Message}");
                return new();
            }
        }

        public async Task<List<EvidenciaDeReporte>> GetEvidenciasDeReporteAsync(string tipo, int id)
        {
            try
            {
                var token = await GetTokenAsync();
                if (string.IsNullOrEmpty(token)) return new();
                string param = tipo switch
                {
                    "electricista" => $"reporteCampoId={id}",
                    "embobinado" => $"reporteEmbobinadoId={id}",
                    "mantenimiento" => $"reporteMantenimientoId={id}",
                    _ => null
                };
                if (param == null) return new();
                var req = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/evidencias?{param}");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var resp = await _httpClient.SendAsync(req);
                if (!resp.IsSuccessStatusCode) return new();
                return await resp.Content.ReadFromJsonAsync<List<EvidenciaDeReporte>>() ?? new();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error GetEvidencias: {ex.Message}");
                return new();
            }
        }

        public async Task<bool> EliminarRefaccionUsadaAsync(int id)
        {
            try
            {
                var token = await GetTokenAsync();
                if (string.IsNullOrEmpty(token)) return false;
                var req = new HttpRequestMessage(HttpMethod.Delete, $"{_baseUrl}/refaccionesusadas/{id}");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var resp = await _httpClient.SendAsync(req);
                return resp.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error EliminarRefaccion: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> EliminarFalloReportadoAsync(int id)
        {
            try
            {
                var token = await GetTokenAsync();
                if (string.IsNullOrEmpty(token)) return false;
                var req = new HttpRequestMessage(HttpMethod.Delete, $"{_baseUrl}/fallosreportados/{id}");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var resp = await _httpClient.SendAsync(req);
                return resp.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error EliminarFallo: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> EliminarEvidenciaAsync(int id)
        {
            try
            {
                var token = await GetTokenAsync();
                if (string.IsNullOrEmpty(token)) return false;
                var req = new HttpRequestMessage(HttpMethod.Delete, $"{_baseUrl}/evidencias/{id}");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var resp = await _httpClient.SendAsync(req);
                return resp.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error EliminarEvidencia: {ex.Message}");
                return false;
            }
        }

        // ============ ELIMINAR REPORTE ============

        public async Task<bool> EliminarReporteAsync(string tipo, int id)
        {
            try
            {
                var token = await GetTokenAsync();
                if (string.IsNullOrEmpty(token)) return false;

                string endpoint = tipo switch
                {
                    "electricista" => $"{_baseUrl}/repelectricista/{id}",
                    "embobinado" => $"{_baseUrl}/repembobinado/{id}",
                    "mantenimiento" => $"{_baseUrl}/repmantenimiento/{id}",
                    _ => null
                };
                if (endpoint == null) return false;

                var req = new HttpRequestMessage(HttpMethod.Delete, endpoint);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var resp = await _httpClient.SendAsync(req);
                return resp.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error EliminarReporte: {ex.Message}");
                return false;
            }
        }
    }

    public class LoginResponse
    {
        public string Token { get; set; }
    }
}