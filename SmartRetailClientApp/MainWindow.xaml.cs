using System.Windows;
using SmartRetailClientApp.Database;
using System.Data;
using SmartRetailClientApp.Logging;
using System.Windows.Threading;
using System.Windows.Controls;
using SmartRetailClientApp.Controllers;
using System.IO;

namespace SmartRetailClientApp
{
    /// <summary>
    /// Clase principal de la ventana principal de la aplicación SmartRetailClientApp.
    /// Controla la interfaz de usuario y coordina sincronización y obtención de datos.
    /// </summary>
    public partial class MainWindow : Window
    {
        // Temporizador para sincronización automática
        private DispatcherTimer syncTimer;

        // Flag para controlar si la sincronización está activa
        private bool isSyncActive = false;

        // Flag para evitar reentradas en sincronización
        private bool isSyncRunning = false;

        // Controlador para conexión con base de datos
        private IDbConnector _dbConnector;

        // Controlador para operaciones de sincronización
        private SyncController _syncController;

        // Controla si ya se pidió login para sincronización automática
        private bool loginSolicitado = false;

        // Diccionario para almacenar datos obtenidos de tablas
        private Dictionary<string, IEnumerable<object>> _obtainedData = new Dictionary<string, IEnumerable<object>>();



        #region Constructor y configuración inicial

        /// <summary>
        /// Constructor de la ventana principal.
        /// Inicializa componentes, carga checkboxes y configura temporizador.
        /// </summary>
        /// <param name="dbConnector">Conector de base de datos</param>
        public MainWindow(IDbConnector dbConnector)
        {
            InitializeComponent();

            _dbConnector = dbConnector;
            _syncController = new SyncController(_dbConnector);

            LoadTableCheckboxes();

            // Inicializa el temporizador para sincronización automática
            syncTimer = new DispatcherTimer();
            syncTimer.Tick += SyncTimer_Tick;

            UpdateStatus("Sincronización desactivada");

            // Establece intervalo por defecto en 10 minutos
            SetSyncInterval(10);

            // Actualiza disponibilidad de controles según estado inicial
            UpdateControlsAvailability();
        }

        /// <summary>
        /// Establece el intervalo del temporizador de sincronización.
        /// </summary>
        /// <param name="minutes">Intervalo en minutos</param>
        private void SetSyncInterval(int minutes)
        {
            syncTimer.Interval = TimeSpan.FromMinutes(minutes);
        }

        #endregion


        #region Eventos y métodos relacionados con sincronización automática

        /// <summary>
        /// Evento del temporizador que ejecuta el proceso de sincronización periódica.
        /// </summary>
        private async void SyncTimer_Tick(object sender, EventArgs e)
        {
            // Evita que se ejecute si ya hay una sincronización en curso
            if (isSyncRunning) return;

            try
            {
                isSyncRunning = true;
                UpdateStatus("Sincronizando...");
                await RunSyncProcess();
                UpdateStatus($"Última sincronización: {DateTime.Now:HH:mm:ss}");
            }
            finally
            {
                isSyncRunning = false;
            }
        }

        /// <summary>
        /// Evento Click del botón para activar o desactivar sincronización automática.
        /// Maneja login inicial y arranque/parada del temporizador.
        /// </summary>
        private async void ToggleSync_Click(object sender, RoutedEventArgs e)
        {
            if (isSyncActive)
            {
                // Detener sincronización automática
                syncTimer.Stop();
                isSyncActive = false;
                UpdateStatus("Sincronización desactivada");
                NotifyUser("Sincronización automática detenida.");
            }
            else
            {
                // Pedir login sólo la primera vez que se activa sincronización automática
                if (!loginSolicitado)
                {
                    bool loggedIn = await _syncController.EnsureApiLogin();
                    if (!loggedIn)
                    {
                        NotifyUser("Inicio de sesión requerido para activar la sincronización.");
                        return;
                    }
                    loginSolicitado = true;
                }

                // Obtener intervalo seleccionado en minutos
                var content = ((ComboBoxItem)SyncIntervalComboBox.SelectedItem).Content.ToString();
                var numberPart = content.Split(' ')[0];

                if (int.TryParse(numberPart, out int interval))
                {
                    SetSyncInterval(interval);
                    syncTimer.Start();
                    isSyncActive = true;
                    UpdateStatus($"Sincronización activada cada {interval} minuto(s)");
                    NotifyUser("Sincronización automática activada.");
                }
                else
                {
                    NotifyUser("Selecciona un intervalo válido.");
                }
            }
        }

        /// <summary>
        /// Evento Click del botón para sincronización manual.
        /// Ejecuta el proceso de sincronización inmediatamente.
        /// </summary>
        private async void ManualSync_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Sincronizando manualmente...");
            await RunSyncProcess();
            UpdateStatus($"Última sincronización: {DateTime.Now:HH:mm:ss}");
        }

        /// <summary>
        /// Ejecuta la sincronización con las tablas seleccionadas.
        /// Verifica login, obtiene tablas seleccionadas y realiza la sincronización.
        /// </summary>
        private async Task RunSyncProcess()
        {
            // Verificar que el usuario esté autenticado en la API
            if (!await _syncController.EnsureApiLogin())
            {
                Logger.WriteLog("No se pudo iniciar sesión. Cancelando sincronización.");
                return;
            }

            // Obtener lista de tablas seleccionadas para sincronizar
            var tablesToSync = GetSelectedTables();

            Logger.WriteLog($"Tablas seleccionadas para sincronizar: {string.Join(", ", tablesToSync)}");

            // Ejecutar sincronización y obtener resultado
            bool success = await _syncController.SyncTablesAsync(tablesToSync);

            if (success)
            {
                NotifyUser("Sincronización completada.");
                UpdateStatus($"Última sincronización: {DateTime.Now:HH:mm:ss}");
            }
            else
            {
                NotifyUser("Error en la sincronización.");
                UpdateStatus("Sincronización fallida.");
            }
        }

        #endregion


        #region Métodos para obtención y visualización de datos

        /// <summary>
        /// Ejecuta la obtención de datos de las tablas seleccionadas mediante llamadas GET.
        /// Llena el diccionario con los datos y actualiza la UI.
        /// </summary>
        private async Task RunGetProcess()
        {
            var selectedTables = GetSelectedTables();

            // Si no hay tablas seleccionadas, asignar tabla por defecto
            if (selectedTables.Count == 0)
            {
                selectedTables = new List<string> { "cliente", "producto", "venta", "detalle_venta" };
            }

            _obtainedData.Clear();

            // Obtener datos para cada tabla seleccionada mediante SyncController
            foreach (var table in selectedTables)
            {
                switch (table)
                {
                    case "cliente":
                        var clientes = await _syncController.GetClientesAsync();
                        _obtainedData.Add(table, clientes);
                        break;
                    case "producto":
                        var productos = await _syncController.GetProductosAsync();
                        _obtainedData.Add(table, productos);
                        break;
                    case "venta":
                        var ventas = await _syncController.GetVentasAsync();
                        _obtainedData.Add(table, ventas);
                        break;
                    case "detalle_venta":
                        var detalles = await _syncController.GetDetallesVentaAsync();
                        _obtainedData.Add(table, detalles);
                        break;
                }
            }

            // Actualizar ComboBox con nombres de tablas disponibles para mostrar
            TablesComboBox.ItemsSource = _obtainedData.Keys;

            // Seleccionar la primera tabla para mostrar por defecto
            if (_obtainedData.Keys.Any())
            {
                TablesComboBox.SelectedIndex = 0;
            }

            NotifyUser("Datos obtenidos y listos para mostrar.");
        }

        /// <summary>
        /// Evento Click del botón "Mostrar Datos".
        /// Verifica login, obtiene datos y los muestra en el DataGrid.
        /// </summary>
        private async void GetDataButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Obteniendo datos...");

            try
            {
                // Verificar login en API antes de obtener datos
                if (!await _syncController.EnsureApiLogin())
                {
                    NotifyUser("Inicio de sesión requerido para obtener datos.");
                    UpdateStatus("Login requerido. Operación cancelada.");
                    return;
                }

                await RunGetProcess();
            }
            catch (Exception ex)
            {
                NotifyUser("Error al obtener datos: " + ex.Message);
                UpdateStatus("Error al obtener datos.");
                Logger.WriteLog($"Excepción en GetDataButton_Click: {ex.Message}");
            }
        }

        /// <summary>
        /// Evento de cambio de selección en ComboBox de tablas.
        /// Actualiza el DataGrid con los datos correspondientes a la tabla seleccionada.
        /// </summary>
        private void TablesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TablesComboBox.SelectedItem is string selectedTable && _obtainedData.ContainsKey(selectedTable))
            {
                DataGridResults.ItemsSource = _obtainedData[selectedTable];
            }
        }

        /// <summary>
        /// Evento que se dispara al generar columnas automáticamente en el DataGrid.
        /// Cancela la generación de columnas no deseadas (como la propiedad completa "Cliente").
        /// </summary>
        private void DataGridResults_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "Cliente")
            {
                e.Cancel = true;
            }
        }

        #endregion


        #region Métodos auxiliares y carga inicial

        /// <summary>
        /// Carga dinámicamente los checkboxes con nombres de tablas obtenidos desde la base de datos.
        /// </summary>
        private void LoadTableCheckboxes()
        {
            try
            {
                Logger.WriteLog("Cargando checkboxes desde la base de datos...");

                var tableNames = _dbConnector.GetTableNames();

                TableCheckboxPanel.Children.Clear();

                foreach (var table in tableNames.OrderBy(t => t))
                {
                    var cb = new CheckBox
                    {
                        Content = table,
                        Margin = new Thickness(5, 2, 5, 2),
                        IsChecked = false
                    };
                    TableCheckboxPanel.Children.Add(cb);
                }

                Logger.WriteLog($"Se cargaron {tableNames.Count} checkboxes: {string.Join(", ", tableNames)}");
            }
            catch (Exception ex)
            {
                NotifyUser("Error al cargar tablas: " + ex.Message);
                Logger.WriteLog($"Error al cargar checkboxes: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene la lista de tablas seleccionadas en los checkboxes.
        /// Si no hay ninguna seleccionada, devuelve la lista por defecto con todas las tablas.
        /// También registra en log el estado de los checkboxes.
        /// </summary>
        /// <returns>Lista de tablas seleccionadas</returns>
        public List<string> GetSelectedTables()
        {
            Logger.WriteLog("Estado de checkboxes:");

            var selectedTables = new List<string>();

            foreach (var child in TableCheckboxPanel.Children)
            {
                if (child is CheckBox cb && cb.IsChecked == true)
                {
                    Logger.WriteLog($"{cb.Content} => ✔️");
                    selectedTables.Add(cb.Content.ToString().ToLower()); // aseguramos minúsculas
                }
                else if (child is CheckBox cbUnchecked)
                {
                    Logger.WriteLog($"{cbUnchecked.Content} => ❌");
                }
            }

            // Si no hay tablas seleccionadas, usar todas por defecto
            if (selectedTables.Count == 0)
            {
                var allTables = new List<string> { "cliente", "detalle_venta", "producto", "venta" };
                Logger.WriteLog("No se seleccionaron tablas, se seleccionan todas por defecto.");
                selectedTables = allTables;
            }

            Logger.WriteLog("Final de GetSelectedTables()");
            Logger.WriteLog($"Total de tablas seleccionadas: {selectedTables.Count}");

            return selectedTables;
        }

        /// <summary>
        /// Actualiza el texto de estado en la UI.
        /// </summary>
        /// <param name="message">Mensaje a mostrar</param>
        private void UpdateStatus(string message)
        {
            StatusTextBlock.Text = "Estado: " + message;
        }

        /// <summary>
        /// Muestra una ventana de mensaje al usuario.
        /// </summary>
        /// <param name="message">Mensaje a mostrar</param>
        private void NotifyUser(string message)
        {
            MessageBox.Show(message, "SmartRetailClientApp");
        }

        /// <summary>
        /// Actualiza la disponibilidad (habilitado/deshabilitado) de botones principales
        /// según si hay tablas cargadas para sincronizar o mostrar.
        /// </summary>
        private void UpdateControlsAvailability()
        {
            bool hasTables = TableCheckboxPanel.Children.Count > 0;

            ManualSyncButton.IsEnabled = hasTables;
            ToggleSyncButton.IsEnabled = hasTables;
            GetDataButton.IsEnabled = hasTables;
        }

        /// <summary>
        /// Abre el explorador de archivos en la carpeta donde se guardan los logs.
        /// </summary>
        private void ViewLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppLogs");
                if (!Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = logDir,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                NotifyUser("No se pudo abrir el explorador: " + ex.Message);
                Logger.WriteLog("Error al abrir explorador de archivos: " + ex.Message);
            }
        }

        #endregion
    }
}
