using SICRY_APP.ViewModels;

namespace SICRY_APP.Views
{
    public partial class ReportFormPage : ContentPage
    {
        public ReportFormPage()
        {
            InitializeComponent();
            BindingContext = new ReportFormViewModel();
        }
    }
}