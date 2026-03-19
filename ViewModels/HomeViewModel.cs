using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SICRY_APP.Models; // Importamos tus modelos (Reporte y CalendarDay)
using System.Collections.ObjectModel;

namespace SICRY_APP.ViewModels
{
    public partial class HomeViewModel : ObservableObject
    {
        // ObservableCollection es una lista especial que avisa a la pantalla automáticamente cuando agregas o quitas elementos
        [ObservableProperty]
        private ObservableCollection<CalendarDay> days;

        [ObservableProperty]
        private ObservableCollection<Reporte> tareas;

        public HomeViewModel()
        {
            Days = new ObservableCollection<CalendarDay>();
            Tareas = new ObservableCollection<Reporte>();

            LoadDays();
            LoadMockTareas(); // Datos de prueba provisionales
        }

        private void LoadDays()
        {
            // Generamos los días de tu diseño original
            Days.Add(new CalendarDay { Day = "23", DayName = "Wed", IsSelected = false });
            Days.Add(new CalendarDay { Day = "24", DayName = "Thu", IsSelected = false });
            Days.Add(new CalendarDay { Day = "25", DayName = "Fri", IsSelected = true }); // Seleccionado por defecto
            Days.Add(new CalendarDay { Day = "26", DayName = "Sat", IsSelected = false });
            Days.Add(new CalendarDay { Day = "27", DayName = "Sun", IsSelected = false });
        }

        private void LoadMockTareas()
        {
            // Aquí simulamos las tareas que luego traerás de tu API de SQL Server
            Tareas.Add(new Reporte
            {
                Titulo = "Mantenimiento Preventivo",
                Descripcion = "Revisar y realizar mantenimiento preventivo del motor en Pozo Norte-12.",
                Ubicacion = "Pozo Norte-12",
                Estado = "Por hacer"
            });
            // Puedes agregar más tareas de prueba aquí si lo deseas
        }

        // Este Comando reemplaza al evento OnDaySelected que tenías en el XAML original
        [RelayCommand]
        private void SelectDay(CalendarDay selectedDay)
        {
            if (selectedDay == null) return;

            // Desmarcar todos y marcar solo el seleccionado
            foreach (var day in Days)
            {
                day.IsSelected = (day == selectedDay);
            }
        }
    }
}