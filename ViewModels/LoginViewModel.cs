using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace SICRY_APP.ViewModels
{
    // 1. "partial" permite que el paquete que instalamos agregue código invisible por nosotros.
    // 2. "ObservableObject" le da a esta clase el poder de avisarle a la pantalla cuando un texto cambia.
    public partial class LoginViewModel : ObservableObject
    {
        // [ObservableProperty] es magia del Toolkit. 
        // Toma esta variable en minúscula (userName) y crea una propiedad pública en mayúscula (UserName) 
        // que la pantalla podrá leer y escribir automáticamente.
        [ObservableProperty]
        private string userName;

        [ObservableProperty]
        private string password;

        // [RelayCommand] convierte este método en un "Comando" llamado LoginCommand.
        // Los botones en la pantalla usarán ese comando en lugar del tradicional evento "Clicked".
        [RelayCommand]
        private async Task LoginAsync()
        {
            // Esta es la misma lógica que tenías en tu proyecto anterior, 
            // pero ahora está separada y segura en el ViewModel.
            if (UserName == "admin" && Password == "1234")
            {
                // Ponemos la primera letra en mayúscula
                string formattedName = char.ToUpper(UserName[0]) + UserName.Substring(1);

                // Preparamos la pantalla principal (AppShell)
                var shell = new AppShell();

                // Si tienes tu método SetUsuario en AppShell, esto le pasará el nombre
                AppShell.Instance?.SetUsuario(formattedName);

                // Cambiamos la pantalla actual por la pantalla principal
                Application.Current.MainPage = shell;
            }
            else
            {
                // Si los datos son incorrectos, mostramos una alerta
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Usuario o contraseña inválidos.", "OK");
                }
            }
        }
    }
}