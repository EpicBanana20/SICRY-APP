using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using SICRY_APP.Models;

namespace SICRY_APP.ViewModels
{
    public partial class CalendarViewModel : ObservableObject
    {
        // ObservableCollection implementa INotifyCollectionChanged, notificando a la interfaz cuando se agregan o eliminan elementos.
        [ObservableProperty]
        private ObservableCollection<Reporte> reportes;

        public CalendarViewModel()
        {
            Reportes = new ObservableCollection<Reporte>();
            LoadReportesMock();
        }

        private void LoadReportesMock()
        {
            // Instanciación de datos estáticos para validación de la interfaz antes de la integración con la API Web.
            Reportes.Add(new Reporte
            {
                Titulo = "Inspección de Válvulas",
                Estado = "Completado",
                Ubicacion = "Pozo Sur-5",
                Descripcion = "Se revisaron las válvulas de presión y se reemplazó el sello principal.",
                ColorEstadoFondo = "#E8F5E9",
                ColorEstadoTexto = "#2E7D32"
            });

            Reportes.Add(new Reporte
            {
                Titulo = "Falla Eléctrica",
                Estado = "Pendiente",
                Ubicacion = "Estación Central",
                Descripcion = "Reporte de variaciones de voltaje en el tablero de control B.",
                ColorEstadoFondo = "#FFF3E0",
                ColorEstadoTexto = "#E65100"
            });
        }

        // Genera un ICommand asíncrono para la acción de edición.
        [RelayCommand]
        private async Task EditarReporteAsync(Reporte reporteSeleccionado)
        {
            if (reporteSeleccionado == null) return;

            // La lógica de enrutamiento (Routing) hacia ReportFormPage se implementará aquí posteriormente.
            await App.Current.MainPage.DisplayAlert("Editar", $"Preparando edición para: {reporteSeleccionado.Titulo}", "OK");
        }

        // Genera un ICommand síncrono para la eliminación de registros en la colección en memoria.
        [RelayCommand]
        private void BorrarReporte(Reporte reporteSeleccionado)
        {
            if (reporteSeleccionado != null && Reportes.Contains(reporteSeleccionado))
            {
                Reportes.Remove(reporteSeleccionado);
            }
        }
    }
}