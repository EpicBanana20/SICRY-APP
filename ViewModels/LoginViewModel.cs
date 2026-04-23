using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using SICRY_APP.Services;

namespace SICRY_APP.ViewModels
{
	public partial class LoginViewModel : ObservableObject
	{
		// Propiedades reactivas para la interfaz
		[ObservableProperty]
		private string userName;

		[ObservableProperty]
		private string password;

		[RelayCommand]
		private async Task LoginAsync()
		{
			// 1. Validación básica de campos vacíos
			if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(Password))
			{
				if (Application.Current?.MainPage != null)
				{
					await Application.Current.MainPage.DisplayAlert("Aviso", "Por favor ingresa usuario y contraseña.", "OK");
				}
				return;
			}

			// 2. Hacer login contra la API (ApiService.Instance es el singleton)
			bool loginExitoso = await ApiService.Instance.LoginAsync(UserName, Password);
			var token = await ApiService.Instance.GetTokenAsync();
			System.Diagnostics.Debug.WriteLine($"========== TOKEN: {token} ==========");

			// 3. Evaluar el resultado
			if (loginExitoso)
			{
                Application.Current.MainPage = new AppShell();
            }
			else
			{
				// ERROR: Credenciales inválidas o servidor apagado
				if (Application.Current?.MainPage != null)
				{
					await Application.Current.MainPage.DisplayAlert("Error", "Usuario o contraseña incorrectos, o no se pudo contactar al servidor.", "OK");
				}
			}
		}
	}
}