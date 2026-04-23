using Microsoft.Maui.Controls;
using SICRY_APP.Services;

namespace SICRY_APP
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // 1. Iniciamos la app temporalmente con una página en blanco o el Login
            MainPage = new NavigationPage(new ContentPage { Title = "Cargando..." });

            // 2. Mandamos a llamar a nuestro guardia de seguridad
            VerificarSesionAutomatica();
        }

        private async void VerificarSesionAutomatica()
        {
            // Le pedimos el token al servicio (el cual lo busca en el SecureStorage)
            var token = await ApiService.Instance.GetTokenAsync();

            if (!string.IsNullOrEmpty(token))
            {
                // ¡TIENE TOKEN! Nos saltamos el Login y vamos directo a la aplicación
                MainPage = new AppShell();
            }
            else
            {
                // NO TIENE TOKEN (o cerró sesión). Lo mandamos a la pantalla de Login
                MainPage = new Views.LoginPage();
            }
        }
    }
}