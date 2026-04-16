using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using SICRY_APP.Services; // Importante para usar el ApiService

namespace SICRY_APP.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        // Propiedades reactivas para la interfaz
        [ObservableProperty]
        private string userName;

        [ObservableProperty]
        private string password;

        [RelayCommand]
        private async Task LoginAsync()
        {
            // 1. Validación básica de campos vacíos
            if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(Password))
            {
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Aviso", "Por favor ingresa usuario y contraseña.", "OK");
                }
                return;
            }

            // 2. Intentar el inicio de sesión con la API
            var apiService = new ApiService();
            var token = await apiService.LoginAsync(UserName, Password);

            // 3. Evaluar el resultado
            if (!string.IsNullOrEmpty(token))
            {
                // ÉXITO: El servidor devolvió un Token JWT válido

                // Formateamos el nombre para mostrarlo en la App (Primera letra mayúscula)
                string formattedName = char.ToUpper(UserName[0]) + UserName.Substring(1);

                // Configuramos la navegación a la pantalla principal
                var shell = new AppShell();

                // Pasamos el nombre del usuario al AppShell si la instancia existe
                AppShell.Instance?.SetUsuario(formattedName);

                // Cambiamos la página principal de la App
                Application.Current.MainPage = shell;
            }
            else
            {
                // ERROR: Credenciales inválidas o el servidor está apagado
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Usuario o contraseña incorrectos, o no se pudo contactar al servidor.", "OK");
                }
            }
        }
    }
}