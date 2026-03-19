using Microsoft.Maui.Controls;
using SICRY_APP.ViewModels; // Importamos la carpeta donde está nuestro ViewModel

namespace SICRY_APP.Views
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent();

            // Esta línea es la conexión oficial de MVVM
            BindingContext = new LoginViewModel();
        }
    }
}