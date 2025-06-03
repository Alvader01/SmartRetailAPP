using System.Windows;

namespace SmartRetailClientApp
{
    public partial class ApiLoginWindow : Window
    {
        // Propiedades para obtener el usuario y contraseña ingresados
        public string Username => UsernameBox.Text;
        public string Password => PasswordBox.Password;

        public ApiLoginWindow()
        {
            InitializeComponent();
            UsernameBox.Text = "";
            PasswordBox.Password = "";
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            // Ocultar mensaje de error previo
            ErrorTextBlock.Visibility = Visibility.Collapsed;
            ErrorTextBlock.Text = "";

            // Validar que ambos campos no estén vacíos
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorTextBlock.Text = "Por favor, rellena todos los campos.";
                ErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }

            // Si todo está bien, se indica resultado OK y se cierra la ventana
            DialogResult = true;
            Close();
        }
    }
}
