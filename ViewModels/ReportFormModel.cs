using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using SICRY_APP.Models;
using System.Threading.Tasks;

namespace SICRY_APP.ViewModels
{
    // QueryProperty es la forma en que MVVM recibe parámetros desde otra pantalla.
    // Aquí le decimos: "Si me mandan algo llamado 'ReporteSeleccionado', guárdalo en mi variable ReporteAEditar"
    [QueryProperty(nameof(ReporteAEditar), "ReporteSeleccionado")]
    public partial class ReportFormViewModel : ObservableObject
    {
        [ObservableProperty]
        private string titulo;

        [ObservableProperty]
        private string ubicacion;

        [ObservableProperty]
        private string descripcion;

        // Esta propiedad guardará el reporte si venimos desde el botón "Editar"
        [ObservableProperty]
        private Reporte reporteAEditar;

        // Este método es "mágico": se ejecuta automáticamente en cuanto ReporteAEditar recibe datos
        partial void OnReporteAEditarChanged(Reporte value)
        {
            if (value != null)
            {
                // Llenamos los campos de texto con los datos del reporte que vamos a editar
                Titulo = value.Titulo;
                Ubicacion = value.Ubicacion;
                Descripcion = value.Descripcion;
            }
        }

        [RelayCommand]
        private async Task GuardarAsync()
        {
            // Más adelante, aquí irá el código de tu ApiService para guardar en Microsoft SQL Server.
            // Por ahora, simulamos que se guarda y mostramos un mensaje.
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Éxito", "Reporte guardado correctamente.", "OK");
            }

            // Shell.Current.GoToAsync("..") significa "Navegar hacia atrás" (regresar a la lista)
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        private async Task CancelarAsync()
        {
            // Si el usuario se arrepiente, solo navegamos hacia atrás sin guardar
            await Shell.Current.GoToAsync("..");
        }
    }
}