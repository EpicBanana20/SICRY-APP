using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SICRY_APP.Models;
using SICRY_APP.Services;

namespace SICRY_APP.ViewModels
{
    public partial class ProfileViewModel : ObservableObject
    {
        [ObservableProperty] private string nombreUsuario;
        [ObservableProperty] private string rolUsuario;
        [ObservableProperty] private string inicial;
        [ObservableProperty] private string colorGafete = "#009688"; // Teal por defecto

        [ObservableProperty] private string totalCompletados = "0";
        [ObservableProperty] private string totalPendientes = "0";

        [ObservableProperty] private bool isBusy;

        public ProfileViewModel()
        {
            // Opcional: poner valores por defecto mientras carga
            NombreUsuario = "Cargando...";
            RolUsuario = "...";
            Inicial = "";
        }

        [RelayCommand]
        public async Task CargarPerfilAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                // 1. Obtener datos del usuario desde el Token
                var perfil = await ApiService.Instance.GetPerfilDesdeTokenAsync();
                if (perfil != null)
                {                    string nombreReal = Preferences.Default.Get("user_name", "Usuario");
                    NombreUsuario = nombreReal;
                    RolUsuario = perfil.RolNombre;
                    Inicial = !string.IsNullOrEmpty(perfil.NombreCompleto)
                              ? perfil.NombreCompleto.Substring(0, 1).ToUpper()
                              : "U";

                    // Colores institucionales según la especialidad
                    ColorGafete = perfil.RolNombre?.ToLower() switch
                    {
                        "electricista" => "#1E88E5", // Azul vibrante
                        "embobinador" => "#FB8C00",  // Naranja
                        "mecanico" or "mantenimiento" => "#546E7A", // Gris azulado
                        "supervisor" => "#8E24AA",   // Morado oscuro
                        _ => "#009688"               // Verde Teal (Defecto)
                    };
                }

                // 2. Obtener Estadísticas (Contar sus asignaciones)
                var misAsignaciones = await ApiService.Instance.GetMisAsignacionesAsync();

                int pendientes = misAsignaciones.Count(a => a.Estado == "Pendiente" || a.Estado == "Inconclusa");
                int completadas = misAsignaciones.Count(a => a.Estado == "Completada");

                TotalPendientes = pendientes.ToString();
                TotalCompletados = completadas.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando perfil: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task CerrarSesionAsync()
        {
            bool respuesta = await Application.Current.MainPage.DisplayAlert(
                "Cerrar Sesión",
                "¿Estás seguro de que deseas salir de tu cuenta?",
                "Sí, salir",
                "Cancelar");

            if (respuesta)
            {
                // 1. Borrar el token y limpiar los datos de sesión en el servicio
                ApiService.Instance.Logout();

                // 2. Método destructor seguro: Cambiar la página principal de la app de golpe
                Application.Current.MainPage = new Views.LoginPage();
            }
        }
    }
}