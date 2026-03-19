using CommunityToolkit.Mvvm.ComponentModel;

namespace SICRY_APP.Models
{
    // Heredar de ObservableObject permite que la interfaz gráfica se actualice automáticamente si el dato cambia
    public partial class CalendarDay : ObservableObject
    {
        [ObservableProperty]
        private string day;

        [ObservableProperty]
        private string dayName;

        [ObservableProperty]
        private bool isSelected;
    }
}