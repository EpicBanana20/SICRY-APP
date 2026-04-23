using SICRY_APP.Views;

namespace SICRY_APP
{
    public partial class AppShell : Shell
    {
        public static AppShell Instance { get; private set; }

        public AppShell()
        {
            InitializeComponent();
            Instance = this;

            // NUEVO: Intentar cargar el nombre guardado automáticamente
            var nombreGuardado = Preferences.Default.Get("user_name", "");
            if (!string.IsNullOrEmpty(nombreGuardado))
            {
                SetUsuario(nombreGuardado);
            }
        }

        public void SetUsuario(string nombre)
        {
            // Asegúrate de que el nombre del Label en tu AppShell.xaml sea "lblUsuario"
            if (lblUser != null)
            {
                lblUser.Text = $"{nombre}";
            }
        }
    }
}