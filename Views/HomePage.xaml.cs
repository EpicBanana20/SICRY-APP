using Microsoft.Maui.Controls;
using SICRY_APP.ViewModels;

namespace SICRY_APP.Views
{
    public partial class HomePage : ContentPage
    {
        public HomePage()
        {
            InitializeComponent();

            // Conexión exclusiva con el ViewModel
            BindingContext = new HomeViewModel();
        }
    }
}