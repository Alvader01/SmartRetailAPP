using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using SmartRetailClientApp.Services;

namespace SmartRetailClientApp
{
    public partial class LoginWindow : Window
    {
        #region Campos y servicios

        private readonly LoginService loginService = new LoginService();

        #endregion

        #region Constructor y eventos iniciales

        public LoginWindow()
        {
            InitializeComponent();

            // Suscripción a eventos de los checkbox para actualizar la UI
            chkIntegratedSecurity.Checked += ChkIntegratedSecurity_CheckedChanged;
            chkIntegratedSecurity.Unchecked += ChkIntegratedSecurity_CheckedChanged;
            chkSaveCredentials.Unchecked += ChkSaveCredentials_Unchecked;

            // Ajustar inputs según estado de seguridad integrada
            UpdateCredentialInputs();

            // Cargar credenciales guardadas (si existen) y rellenar controles
            var saved = CredentialStorage.LoadCredentials();
            if (saved != null)
            {
                txtHost.Text = saved.Host;
                txtDatabase.Text = saved.Database;
                txtUser.Text = saved.User;
                txtPassword.Password = saved.Password;
                chkIntegratedSecurity.IsChecked = saved.UseIntegratedSecurity;
                chkSaveCredentials.IsChecked = true;

                // Actualizar inputs según la seguridad integrada cargada
                UpdateCredentialInputs();
            }
        }

        #endregion

        #region Eventos de controles

        /// <summary>
        /// Evento disparado al cambiar el estado del checkbox de seguridad integrada.
        /// Actualiza el estado de los campos de usuario y contraseña.
        /// </summary>
        private void ChkIntegratedSecurity_CheckedChanged(object sender, RoutedEventArgs e)
        {
            UpdateCredentialInputs();
        }

        /// <summary>
        /// Evento disparado al desmarcar el checkbox de guardar credenciales.
        /// Pregunta confirmación para borrar credenciales guardadas y actúa según respuesta.
        /// </summary>
        private void ChkSaveCredentials_Unchecked(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("¿Seguro que desea borrar las credenciales guardadas?",
                                         "Confirmar borrado",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // Borra las credenciales guardadas
                CredentialStorage.ClearCredentials();
            }
            else
            {
                // Si el usuario cancela, vuelve a marcar el checkbox para no perder credenciales
                chkSaveCredentials.IsChecked = true;
            }
        }

        /// <summary>
        /// Evento disparado al hacer clic en el botón Conectar.
        /// Gestiona el proceso de validación y conexión a la base de datos de forma asincrónica.
        /// </summary>
        private async void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            // Deshabilitar botón para evitar múltiples clics y mostrar el spinner
            btnConnect.IsEnabled = false;
            progressSpinner.Visibility = Visibility.Visible;

            // Obtener valores de los controles
            string host = txtHost.Text.Trim();
            string database = txtDatabase.Text.Trim();
            string user = txtUser.Text.Trim();
            string password = txtPassword.Password;
            bool useIntegratedSecurity = chkIntegratedSecurity.IsChecked == true;

            // Validar los datos de entrada
            var validation = loginService.ValidateInputs(host, database, user, useIntegratedSecurity);
            if (!validation.IsValid)
            {
                // Mostrar mensaje de error y restaurar UI si la validación falla
                MessageBox.Show(validation.Message, "Validación");
                btnConnect.IsEnabled = true;
                progressSpinner.Visibility = Visibility.Collapsed;
                return;
            }

            try
            {
                // Ejecutar la conexión en un Task para no bloquear la UI
                var connector = await Task.Run(() =>
                    loginService.Connect(host, database, user, password, useIntegratedSecurity));

                // Guardar o borrar credenciales según selección del usuario
                if (chkSaveCredentials.IsChecked == true)
                    CredentialStorage.SaveCredentials(host, database, user, password, useIntegratedSecurity);
                else
                    CredentialStorage.ClearCredentials();

                // Abrir la ventana principal y cerrar la de login
                var mainWindow = new MainWindow(connector);
                mainWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                // Mostrar error amigable si la conexión falla
                string friendlyMsg = loginService.GetFriendlyErrorMessage(ex);
                MessageBox.Show($"Error al conectar: {friendlyMsg}", "Conexión fallida");

                // Reactivar botón y ocultar spinner para permitir nuevo intento
                btnConnect.IsEnabled = true;
                progressSpinner.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region Métodos privados auxiliares

        /// <summary>
        /// Actualiza el estado habilitado/deshabilitado de los campos usuario y contraseña
        /// según si se usa o no seguridad integrada.
        /// </summary>
        private void UpdateCredentialInputs()
        {
            bool integrated = chkIntegratedSecurity.IsChecked == true;

            // Si se usa seguridad integrada, deshabilitar usuario y contraseña
            txtUser.IsEnabled = !integrated;
            txtPassword.IsEnabled = !integrated;
        }

        #endregion

        #region Clase anidada para almacenamiento de credenciales

        /// <summary>
        /// Clase estática que maneja el almacenamiento seguro de credenciales en un archivo JSON cifrado.
        /// </summary>
        public static class CredentialStorage
        {
            private static readonly string FilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SmartRetailClientApp", "credentials.json");

            /// <summary>
            /// Guarda las credenciales en archivo local cifrado.
            /// </summary>
            public static void SaveCredentials(string host, string database, string user, string password, bool integrated)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));

                var creds = new StoredCredentials
                {
                    Host = host,
                    Database = database,
                    User = user,
                    Password = Encrypt(password),
                    UseIntegratedSecurity = integrated
                };

                string json = JsonSerializer.Serialize(creds);
                File.WriteAllText(FilePath, json);
            }

            /// <summary>
            /// Carga las credenciales guardadas y las descifra.
            /// Retorna null si no existen o si ocurre un error.
            /// </summary>
            public static StoredCredentials? LoadCredentials()
            {
                if (!File.Exists(FilePath)) return null;

                try
                {
                    var json = File.ReadAllText(FilePath);
                    var creds = JsonSerializer.Deserialize<StoredCredentials>(json);
                    if (creds != null)
                        creds.Password = Decrypt(creds.Password);

                    return creds;
                }
                catch
                {
                    return null;
                }
            }

            /// <summary>
            /// Borra el archivo con las credenciales guardadas.
            /// </summary>
            public static void ClearCredentials()
            {
                if (File.Exists(FilePath))
                    File.Delete(FilePath);
            }

            /// <summary>
            /// Cifra el texto plano usando protección de datos para el usuario actual.
            /// </summary>
            private static string Encrypt(string plainText)
            {
                if (string.IsNullOrEmpty(plainText)) return "";

                var bytes = Encoding.UTF8.GetBytes(plainText);
                var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encrypted);
            }

            /// <summary>
            /// Descifra el texto cifrado usando protección de datos para el usuario actual.
            /// </summary>
            private static string Decrypt(string encryptedText)
            {
                if (string.IsNullOrEmpty(encryptedText)) return "";

                try
                {
                    var bytes = Convert.FromBase64String(encryptedText);
                    var decrypted = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
                    return Encoding.UTF8.GetString(decrypted);
                }
                catch
                {
                    return "";
                }
            }
        }

        /// <summary>
        /// Clase para mapear las credenciales almacenadas.
        /// </summary>
        public class StoredCredentials
        {
            public string Host { get; set; } = "";
            public string Database { get; set; } = "";
            public string User { get; set; } = "";
            public string Password { get; set; } = "";
            public bool UseIntegratedSecurity { get; set; }
        }

        #endregion
    }
}
