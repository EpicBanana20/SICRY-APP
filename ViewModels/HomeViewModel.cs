using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SICRY_APP.Models;
using SICRY_APP.Services;
using System.Collections.ObjectModel;
using System.Globalization;

namespace SICRY_APP.ViewModels
{
    public partial class HomeViewModel : ObservableObject
    {
        [ObservableProperty] private ObservableCollection<CalendarDay> days;
        [ObservableProperty] private ObservableCollection<Asignacion> tareas;
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private bool hasTareas;

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
                Tareas.Clear();
                var lista = await ApiService.Instance.GetMisAsignacionesAsync();
                foreach (var a in lista) Tareas.Add(a);
                HasTareas = Tareas.Count > 0;
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private void SelectDay(CalendarDay selectedDay)
        {
            if (selectedDay == null) return;
            foreach (var day in Days) day.IsSelected = (day == selectedDay);
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