using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SICRY_APP.Models;
using SICRY_APP.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq; // Necesario para el .Where()

namespace SICRY_APP.ViewModels
{
    public partial class HomeViewModel : ObservableObject
    {
        [ObservableProperty] private ObservableCollection<CalendarDay> days;
        [ObservableProperty] private ObservableCollection<Asignacion> tareas;
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private bool hasTareas;

        // Propiedad para saber qué botón está pintado de color
        [ObservableProperty] private string filtroActual = "Todas";

        // Lista maestra en memoria para no llamar a la API a cada rato
        private List<Asignacion> _todasLasTareas = new();

        public HomeViewModel()
        {
            Days = new ObservableCollection<CalendarDay>();
            Tareas = new ObservableCollection<Asignacion>();
            LoadDays();
            _ = CargarAsignacionesAsync();
        }

        private void LoadDays()
        {
            var cultura = new CultureInfo("es-ES");
            var hoy = DateTime.Now.Date;
            for (int i = -2; i <= 2; i++)
            {
                var fecha = hoy.AddDays(i);
                string nombreDia = cultura.DateTimeFormat.GetAbbreviatedDayName(fecha.DayOfWeek).Replace(".", "");
                nombreDia = char.ToUpper(nombreDia[0]) + nombreDia.Substring(1);
                Days.Add(new CalendarDay { Day = fecha.Day.ToString(), DayName = nombreDia, IsSelected = (i == 0) });
            }
        }

        [RelayCommand]
        private async Task CargarAsignacionesAsync()
        {
            try
            {
                IsBusy = true;
                _todasLasTareas = await ApiService.Instance.GetMisAsignacionesAsync();

                // En lugar de llenarlas directamente, llamamos al filtro para que aplique el que esté seleccionado
                Filtrar(FiltroActual);
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private void SelectDay(CalendarDay selectedDay)
        {
            if (selectedDay == null) return;
            foreach (var day in Days) day.IsSelected = (day == selectedDay);
        }

        // NUEVO COMANDO: Lógica para filtrar las tarjetas
        [RelayCommand]
        private void Filtrar(string filtro)
        {
            FiltroActual = filtro; // Actualizamos para que el botón cambie de color
            Tareas.Clear();

            IEnumerable<Asignacion> listaFiltrada = _todasLasTareas;

            // Mapeamos los botones con los estados de tu base de datos SQL
            if (filtro == "Por hacer")
                listaFiltrada = _todasLasTareas.Where(t => t.Estado == "Pendiente");
            else if (filtro == "Inconclusa")
                listaFiltrada = _todasLasTareas.Where(t => t.Estado == "Inconclusa");
            else if (filtro == "Completadas")
                listaFiltrada = _todasLasTareas.Where(t => t.Estado == "Completada");

            foreach (var tarea in listaFiltrada)
            {
                Tareas.Add(tarea);
            }

            HasTareas = Tareas.Count > 0;
        }

        [RelayCommand]
        private async Task AbrirReporteAsync(Asignacion asignacion)
        {
            if (asignacion == null) return;

            if (asignacion.Estado?.ToLower() == "completada")
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Aviso", "Esta asignación ya está completada.", "OK");
                return;
            }

            var parametros = new Dictionary<string, object> { { "Asignacion", asignacion } };
            await Shell.Current.GoToAsync("ReportFormPage", parametros);
        }
    }
}