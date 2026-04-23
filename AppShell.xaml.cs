using SICRY_APP.Views;

namespace SICRY_APP
{
    public partial class AppShell : Shell
    {
        public static AppShell? Instance { get; private set; }

        public AppShell()
        {
            InitializeComponent();
            Instance = this;

            // Registrar ruta para navegación al formulario de reporte
            Routing.RegisterRoute("ReportFormPage", typeof(ReportFormPage));
        }

        public void SetUsuario(string nombre)
        {
            lblUser.Text = nombre;
        }
    }
}