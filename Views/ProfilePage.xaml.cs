using SICRY_APP.ViewModels;

namespace SICRY_APP.Views
{
    public partial class ProfilePage : ContentPage
    {
        public ProfilePage()
        {
            InitializeComponent();
            BindingContext = new ProfileViewModel();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Cada vez que la pantalla aparece, le decimos al ViewModel que recargue los datos
            if (BindingContext is ProfileViewModel vm)
            {
                _ = vm.CargarPerfilAsync();
            }
        }
    }
}