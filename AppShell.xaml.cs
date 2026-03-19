namespace SICRY_APP
{
    public partial class AppShell : Shell
    {
        public static AppShell? Instance { get; private set; }

        public AppShell()
        {
            InitializeComponent();
            Instance = this;
        }

        // Este método es llamado desde el LoginViewModel cuando la contraseña es correcta
        public void SetUsuario(string nombre)
        {
            lblUser.Text = nombre;
        }
    }
}