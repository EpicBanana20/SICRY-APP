using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SICRY_APP.Services;

namespace SICRY_APP.ViewModels
{
    public partial class ProfileViewModel : ObservableObject
    {
        [ObservableProperty] private string nombreUsuario = "Cargando...";
        [ObservableProperty] private string rolUsuario = "...";
        [ObservableProperty] private string inicial = "";
        [ObservableProperty] private string colorGafete = "#009688";
        [ObservableProperty] private bool isBusy;

        [ObservableProperty] private string horasExtrasSemana = "—";
        [ObservableProperty] private string asignacionesActivas = "—";

        public ProfileViewModel()
        {
            _ = CargarPerfilAsync();
        }

        [RelayCommand]
        public async Task CargarPerfilAsync()
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;

                var perfilTask       = ApiService.Instance.GetPerfilDesdeTokenAsync();
                var horasTask        = ApiService.Instance.GetHorasExtraSemanaAsync();
                var asignacionesTask = ApiService.Instance.GetMisAsignacionesAsync();

                await Task.WhenAll(perfilTask, horasTask, asignacionesTask);

                var perfil = perfilTask.Result;
                if (perfil != null)
                {
                    NombreUsuario = perfil.NombreCompleto;
                    RolUsuario    = perfil.RolNombre;
                    Inicial       = !string.IsNullOrEmpty(perfil.NombreCompleto)
                                    ? perfil.NombreCompleto.Substring(0, 1).ToUpper()
                                    : "U";

                    ColorGafete = perfil.RolNombre?.ToLower() switch
                    {
                        "electricista"                => "#1E88E5",
                        "embobinador"                 => "#FB8C00",
                        "mecanico" or "mantenimiento" => "#546E7A",
                        "supervisor"                  => "#8E24AA",
                        _                             => "#009688"
                    };
                }

                HorasExtrasSemana = $"{horasTask.Result} h";

                var activas = asignacionesTask.Result
                    .Count(a => a.Estado == "Pendiente" || a.Estado == "Inconclusa");
                AsignacionesActivas = activas.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando perfil: {ex.Message}");
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task CerrarSesionAsync()
        {
            bool respuesta = await Application.Current.MainPage.DisplayAlert(
                "Cerrar Sesión",
                "¿Estás seguro de que deseas salir de tu cuenta?",
                "Sí, salir", "Cancelar");

            if (respuesta)
            {
                ApiService.Instance.Logout();
                Application.Current.MainPage = new Views.LoginPage();
            }
        }
    }
}
