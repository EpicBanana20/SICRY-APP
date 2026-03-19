using Microsoft.Maui.Controls;
using SICRY_APP.Views; // Importamos la carpeta de las vistas

namespace SICRY_APP
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Arrancamos la aplicación directamente en la pantalla de Login
            return new Window(new LoginPage());
        }
    }
}