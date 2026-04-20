using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SICRY_APP.Models;
using System.Collections.ObjectModel;
using System.Globalization;

namespace SICRY_APP.ViewModels
{
    public partial class HomeViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<CalendarDay> days;

        [ObservableProperty]
        private ObservableCollection<Reporte> tareas;

        public HomeViewModel()
        {
            Days = new ObservableCollection<CalendarDay>();
            Tareas = new ObservableCollection<Reporte>();

            LoadDays();
            LoadMockTareas();
        }

        private void LoadDays()
        {
            var cultura = new CultureInfo("es-ES");
            var hoy = DateTime.Now.Date;

            for (int i = -2; i <= 2; i++)
            {
                var fecha = hoy.AddDays(i);
                string nombreDia = cultura.DateTimeFormat
                    .GetAbbreviatedDayName(fecha.DayOfWeek)
                    .Replace(".", "");
                nombreDia = char.ToUpper(nombreDia[0]) + nombreDia.Substring(1);

                Days.Add(new CalendarDay
                {
                    Day = fecha.Day.ToString(),
                    DayName = nombreDia,
                    IsSelected = (i == 0)
                });
            }
        }

        private void LoadMockTareas()
        {
            Tareas.Add(new Reporte
            {
                Titulo = "Mantenimiento Preventivo",
                Descripcion = "Revisar y realizar mantenimiento preventivo del motor en Pozo Norte-12.",
                Ubicacion = "Pozo Norte-12",
                Estado = "Por hacer"
            });
        }

        [RelayCommand]
        private void SelectDay(CalendarDay selectedDay)
        {
            if (selectedDay == null) return;
            foreach (var day in Days)
                day.IsSelected = (day == selectedDay);
        }
    }
}