using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IntelliJ.Lang.Annotations;
using Microsoft.Maui.Controls;
using SICRY_APP.Services;
using SICRY_APP.Views;

namespace SICRY_APP.ViewModels
{
	public partial class ProfileViewModel : ObservableObject
	{
		// Propiedades reactivas que se enlazan con la interfaz (XAML)
		[ObservableProperty]
		private string fullName;

		[ObservableProperty]
		private string userName;

		[ObservableProperty]
		private string rol;

		[ObservableProperty]
		private bool isBusy;

		public ProfileViewModel()
		{
			// Valores iniciales mientras se carga la información real
			FullName = "Cargando...";
			UserName = "...";
			Rol = "...";

			// Disparamos la carga desde el token JWT
			_ = CargarDatosUsuarioAsync();
		}

		private async Task CargarDatosUsuarioAsync()
		{
			try
			{
				IsBusy = true;

				// Decodifica el token guardado tras el login y extrae los claims
				var perfil = await ApiService.Instance.GetPerfilDesdeTokenAsync();

				if (perfil != null)
				{
					FullName = perfil.NombreCompleto;
					UserName = perfil.Username;
					Rol = perfil.RolNombre;
				}
				else
				{
					// No hay token o está corrupto
					FullName = "Sin sesión";
					UserName = "desconocido";
					Rol = "N/A";
				}
			}
			finally
			{
				IsBusy = false;
			}
		}

		// ICommand asíncrono para cerrar sesión
		[RelayCommand]
		private async Task CerrarSesionAsync()
		{
			if (Application.Current?.MainPage == null) return;

			bool confirmar = await Application.Current.MainPage.DisplayAlert(
				"Cerrar Sesión",
				"¿Estás seguro de que deseas salir?",
				"Sí", "Cancelar");

			if (!confirmar) return;

			// Borra el token guardado en SecureStorage
			ApiService.Instance.Logout();

			// Regresa al Login
			Application.Current.MainPage = new NavigationPage(new LoginPage());
		}
	}
}