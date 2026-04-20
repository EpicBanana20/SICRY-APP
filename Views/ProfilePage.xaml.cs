using Microsoft.Maui.Controls;
using SICRY_APP.ViewModels;

namespace SICRY_APP.Views
{
	public partial class ProfilePage : ContentPage
	{
		public ProfilePage()
		{
			InitializeComponent();

			// Asignación de la instancia de ProfileViewModel como BindingContext
			BindingContext = new ProfileViewModel();
		}
	}
}