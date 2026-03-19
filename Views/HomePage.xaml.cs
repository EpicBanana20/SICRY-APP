using Microsoft.Maui.Controls;
using SICRY_APP.ViewModels;

namespace SICRY_APP.Views
{
    public partial class CalendarPage : ContentPage
    {
        public CalendarPage()
        {
            InitializeComponent();

            // Asignación de la instancia de CalendarViewModel como BindingContext
            BindingContext = new CalendarViewModel();
        }
    }
}