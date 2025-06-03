using SmartRetailClientApp.Auth;
using SmartRetailClientApp.Database;
using SmartRetailClientApp.Logging;
using SmartRetailClientApp.Transformations;
using SmartRetailClientApp.Uploader;
using SmartRetailClientApp.Config;
using SmartRetailClientApp.Models;
using SmartRetailClientApp.Getter;
using System.Text.Json;
using System.Windows;

namespace SmartRetailClientApp.Controllers
{
    /// <summary>
    /// Controlador principal encargado de la autenticación y sincronización de datos
    /// entre la base de datos local y la API del sistema Smart Retail.
    /// </summary>
    public class SyncController
    {
        private readonly IDbConnector _dbConnector;
        private string _username;
        private string _password;
        private DateTime _tokenExpiration = DateTime.MinValue;

        public SyncController(IDbConnector dbConnector)
        {
            _dbConnector = dbConnector;
        }

        /// <summary>
        /// Valida si el token de autenticación sigue siendo válido.
        /// En caso contrario, solicita credenciales al usuario para autenticarse nuevamente en la API.
        /// </summary>
        public async Task<bool> EnsureApiLogin()
        {
            // Verifica si el token no existe o ha expirado
            if (string.IsNullOrEmpty(AuthManager.Token) || DateTime.Now >= _tokenExpiration)
            {
                while (true)
                {
                    // Si no hay credenciales almacenadas, se abre una ventana para solicitarlas
                    if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_password))
                    {
                        var loginWindow = new ApiLoginWindow();
                        bool? result = loginWindow.ShowDialog();

                        if (result != true)
                            return false; // Usuario canceló el diálogo

                        _username = loginWindow.Username;
                        _password = loginWindow.Password;
                    }

                    // Intenta autenticarse en la API
                    bool loginSuccess = await AuthManager.LoginAsync(_username, _password);

                    if (loginSuccess)
                    {
                        Logger.WriteLog("Inicio de sesión en la API exitoso.");
                        _tokenExpiration = DateTime.Now.AddMinutes(55); // Tiempo estimado de validez del token
                        break;
                    }
                    else
                    {
                        // Si falla la autenticación, se reinician las credenciales y se informa al usuario
                        Logger.WriteLog("Error al iniciar sesión en la API. Credenciales incorrectas.");
                        _username = null;
                        _password = null;
                        MessageBox.Show("Usuario o contraseña incorrectos. Intenta de nuevo.", "Error de autenticación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Sincroniza con la API las tablas locales seleccionadas por el usuario.
        /// Aplica transformación de datos y respeta un orden definido para las relaciones.
        /// </summary>
        public async Task<bool> SyncTablesAsync(List<string> tablesToSync)
        {
            Logger.WriteLog("SyncTablesAsync iniciado...");

            // Verifica sesión válida antes de proceder
            if (!await EnsureApiLogin())
            {
                Logger.WriteLog("Fallo en EnsureApiLogin. Abortando sincronización.");
                return false;
            }

            // Mapeo de nombres de tablas locales a endpoints de la API
            Dictionary<string, string> endpointMap = new()
            {
                { "producto", "productos" },
                { "cliente", "clientes" },
                { "venta", "ventas" },
                { "detalle_venta", "detallesventa" }
            };

            // Define el orden correcto de sincronización (por integridad referencial)
            List<string> syncOrder = new() { "producto", "cliente", "venta", "detalle_venta" };

            if (tablesToSync == null || tablesToSync.Count == 0)
            {
                Logger.WriteLog("No se seleccionaron tablas. Abortando sincronización.");
                return false;
            }

            // Normaliza nombres de tablas ingresadas
            var normalizedTables = tablesToSync.Select(t => t.ToLower()).ToList();

            // Aplica el orden de sincronización solo a las tablas seleccionadas
            var orderedTables = syncOrder.Where(normalizedTables.Contains).ToList();

            Logger.WriteLog($"Tablas que se sincronizarán en orden: {string.Join(", ", orderedTables)}");

            foreach (var table in orderedTables)
            {
                // Obtiene el nombre del endpoint correspondiente
                if (!endpointMap.TryGetValue(table, out string endpointName))
                {
                    Logger.WriteLog($"No se encontró endpoint para la tabla '{table}'. Se omite.");
                    continue;
                }

                // Verifica que la tabla tenga columnas
                var columns = _dbConnector.GetColumnNames(table);
                if (columns.Count == 0)
                {
                    Logger.WriteLog($"La tabla '{table}' no tiene columnas. Se omite.");
                    continue;
                }

                // Obtiene datos no sincronizados
                var data = _dbConnector.GetUnsyncedTableData(table, columns);
                if (data.Rows.Count == 0)
                {
                    Logger.WriteLog($"No hay datos para sincronizar en '{table}'.");
                    continue;
                }

                // Transforma los datos según la lógica definida para esa tabla
                var transformedData = DataTransformer.Transform(data, table);

                // Log opcional de los datos transformados
                try
                {
                    var jsonPreview = JsonSerializer.Serialize(transformedData);
                    Logger.WriteLog($"Datos transformados para tabla '{table}': {jsonPreview}");
                }
                catch (Exception ex)
                {
                    Logger.WriteLog($"Error al serializar datos transformados: {ex.Message}");
                }

                // Construye la URL de carga y realiza la subida
                string uploadUrl = $"{AppConfig.ApiEndpoint}/api/{endpointName}";
                Logger.WriteLog($"Subiendo {data.Rows.Count} registros de '{table}' a: {uploadUrl}");

                bool success = await ApiUploader.UploadDataAsync(transformedData, uploadUrl, AuthManager.Token);

                if (!success)
                {
                    Logger.WriteLog($"Error al sincronizar la tabla '{table}'. Abortando.");
                    return false;
                }

                // Marca como sincronizados los registros
                Logger.WriteLog($"Tabla '{table}' sincronizada correctamente.");
                _dbConnector.MarkRowsAsSynced(table, data);
            }

            Logger.WriteLog("Sincronización completada con éxito.");
            return true;
        }

        /// <summary>
        /// Obtiene la lista de clientes desde la API.
        /// </summary>
        public async Task<List<Cliente>?> GetClientesAsync()
        {
            if (!await EnsureApiLogin()) return null;

            string url = $"{AppConfig.ApiEndpoint}/api/clientes";
            string json = await ApiGetter.GetDataAsync(url, AuthManager.Token);
            if (string.IsNullOrEmpty(json)) return null;

            try
            {
                // Deserializa la respuesta JSON a una lista de objetos Cliente
                var clientes = JsonSerializer.Deserialize<List<Cliente>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return clientes;
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error deserializando clientes: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Obtiene la lista de productos desde la API.
        /// </summary>
        public async Task<List<Producto>?> GetProductosAsync()
        {
            if (!await EnsureApiLogin()) return null;

            string url = $"{AppConfig.ApiEndpoint}/api/productos";
            string json = await ApiGetter.GetDataAsync(url, AuthManager.Token);
            if (string.IsNullOrEmpty(json)) return null;

            try
            {
                // Deserializa JSON a lista de productos
                var productos = JsonSerializer.Deserialize<List<Producto>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return productos;
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error deserializando productos: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Obtiene la lista de ventas desde la API.
        /// </summary>
        public async Task<List<Venta>?> GetVentasAsync()
        {
            if (!await EnsureApiLogin()) return null;

            string url = $"{AppConfig.ApiEndpoint}/api/ventas";
            string json = await ApiGetter.GetDataAsync(url, AuthManager.Token);
            if (string.IsNullOrEmpty(json)) return null;

            try
            {
                var ventas = JsonSerializer.Deserialize<List<Venta>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return ventas;
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error deserializando ventas: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Obtiene la lista de detalles de venta desde la API.
        /// </summary>
        public async Task<List<DetalleVenta>?> GetDetallesVentaAsync()
        {
            if (!await EnsureApiLogin()) return null;

            string url = $"{AppConfig.ApiEndpoint}/api/detallesventa";
            string json = await ApiGetter.GetDataAsync(url, AuthManager.Token);
            if (string.IsNullOrEmpty(json)) return null;

            try
            {
                var detalles = JsonSerializer.Deserialize<List<DetalleVenta>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return detalles;
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Error deserializando detalles venta: {ex.Message}");
                return null;
            }
        }
    }
}
