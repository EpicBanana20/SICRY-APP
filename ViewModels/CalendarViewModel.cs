using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using SICRY_APP.Models;
using SICRY_APP.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace SICRY_APP.ViewModels
{
    public partial class CalendarViewModel : ObservableObject
    {
        [ObservableProperty] private ObservableCollection<ReporteItem> reportes;
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private bool hasReportes;

        public CalendarViewModel()
        {
            Reportes = new ObservableCollection<ReporteItem>();
            _ = CargarReportesAsync();
        }

        [RelayCommand]
        private async Task CargarReportesAsync()
        {
            try
            {
                IsBusy = true;
                Reportes.Clear();
                var lista = await ApiService.Instance.GetMisReportesAsync();
                foreach (var r in lista) Reportes.Add(r);
                HasReportes = Reportes.Count > 0;
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task EditarReporteAsync(ReporteItem reporteSeleccionado)
        {
            if (reporteSeleccionado == null) return;

            var parametros = new Dictionary<string, object> { { "Reporte", reporteSeleccionado } };
            await Shell.Current.GoToAsync("ReportFormPage", parametros);
        }

        [RelayCommand]
        private async Task BorrarReporteAsync(ReporteItem reporteSeleccionado)
        {
            if (reporteSeleccionado == null) return;
            if (Application.Current?.MainPage == null) return;

            bool confirmar = await Application.Current.MainPage.DisplayAlert(
                "Eliminar reporte",
                "¿Estás seguro de que deseas eliminar este reporte?",
                "Sí", "Cancelar");
            if (!confirmar) return;

            var ok = await ApiService.Instance.EliminarReporteAsync(reporteSeleccionado.Tipo, reporteSeleccionado.Id);
            if (ok)
            {
                // Si el reporte era conclusivo, la asignación estaba Completada — revertirla
                if (reporteSeleccionado.EsConclusivo)
                    await ApiService.Instance.CambiarEstadoAsignacionAsync(reporteSeleccionado.IdAsignacion, "Inconclusa");

                Reportes.Remove(reporteSeleccionado);
                HasReportes = Reportes.Count > 0;
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Error", "No se pudo eliminar el reporte.", "OK");
            }
        }
    }
}
