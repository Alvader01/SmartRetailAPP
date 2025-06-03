using System.Data;
using MySql.Data.MySqlClient;

namespace SmartRetailClientApp.Database
{
    /// <summary>
    /// Implementación del conector para bases de datos MySQL.
    /// Provee métodos para abrir/cerrar conexión, obtener tablas, columnas y manejar sincronización.
    /// </summary>
    public class MySqlConnector : IDbConnector, IDisposable
    {
        private readonly string _connectionString;
        private MySqlConnection _connection;

        /// <summary>
        /// Constructor que inicializa el conector con los parámetros de conexión.
        /// </summary>
        /// <param name="host">Host o IP del servidor MySQL.</param>
        /// <param name="database">Nombre de la base de datos.</param>
        /// <param name="user">Usuario para la conexión.</param>
        /// <param name="password">Contraseña del usuario.</param>
        public MySqlConnector(string host, string database, string user, string password)
        {
            _connectionString = $"Server={host};Database={database};Uid={user};Pwd={password};";
        }

        /// <summary>
        /// Escapa nombres de tablas o columnas para evitar problemas con caracteres reservados o especiales.
        /// En MySQL se utilizan backticks.
        /// </summary>
        /// <param name="name">Nombre a escapar.</param>
        /// <returns>Nombre escapado con backticks.</returns>
        private string EscapeName(string name) => $"`{name.Replace("`", "``")}`";

        /// <summary>
        /// Abre la conexión a la base de datos MySQL si no está ya abierta.
        /// </summary>
        public void OpenConnection()
        {
            if (_connection == null)
                _connection = new MySqlConnection(_connectionString);

            if (_connection.State != ConnectionState.Open)
                _connection.Open();
        }

        /// <summary>
        /// Cierra la conexión abierta si existe y está abierta.
        /// </summary>
        public void CloseConnection()
        {
            if (_connection != null && _connection.State != ConnectionState.Closed)
                _connection.Close();
        }

        /// <summary>
        /// Obtiene la lista de nombres de todas las tablas disponibles en la base de datos actual.
        /// </summary>
        /// <returns>Lista de nombres de tablas.</returns>
        public List<string> GetTableNames()
        {
            var tables = new List<string>();

            // Asegurar que la conexión está abierta
            OpenConnection();

            string query = "SHOW TABLES";

            using (var command = new MySqlCommand(query, _connection))
            using (var reader = command.ExecuteReader())
            {
                // Leer todas las tablas retornadas y agregarlas a la lista
                while (reader.Read())
                {
                    tables.Add(reader.GetString(0));
                }
            }

            return tables;
        }

        /// <summary>
        /// Obtiene la lista de nombres de columnas de una tabla específica.
        /// </summary>
        /// <param name="tableName">Nombre de la tabla a consultar.</param>
        /// <returns>Lista de nombres de columnas.</returns>
        public List<string> GetColumnNames(string tableName)
        {
            var columns = new List<string>();

            // Asegurar que la conexión está abierta
            OpenConnection();

            string query = $"SHOW COLUMNS FROM {EscapeName(tableName)}";

            using (var command = new MySqlCommand(query, _connection))
            using (var reader = command.ExecuteReader())
            {
                // Leer todas las columnas y agregarlas a la lista
                while (reader.Read())
                {
                    columns.Add(reader.GetString(0));
                }
            }

            return columns;
        }

        /// <summary>
        /// Obtiene los datos de una tabla específica, con las columnas indicadas.
        /// Si no se indican columnas, obtiene todas.
        /// </summary>
        /// <param name="tableName">Nombre de la tabla a consultar.</param>
        /// <param name="columns">Lista de columnas a obtener.</param>
        /// <returns>Un DataTable con los datos solicitados.</returns>
        public DataTable GetTableData(string tableName, List<string> columns)
        {
            var dt = new DataTable();

            // Asegurar conexión abierta
            OpenConnection();

            // Preparar lista de columnas escapadas o "*" si no hay columnas específicas
            string columnsPart = columns != null && columns.Count > 0
                ? string.Join(", ", columns.ConvertAll(c => EscapeName(c)))
                : "*";

            string query = $"SELECT {columnsPart} FROM {EscapeName(tableName)}";

            using (var command = new MySqlCommand(query, _connection))
            using (var adapter = new MySqlDataAdapter(command))
            {
                // Rellenar DataTable con resultados de la consulta
                adapter.Fill(dt);
            }

            return dt;
        }

        /// <summary>
        /// Obtiene filas no sincronizadas de una tabla, asumiendo columna 'IsSynced' (0 = no sincronizado).
        /// Si no existe esa columna, devuelve todas las filas.
        /// </summary>
        /// <param name="tableName">Tabla a consultar.</param>
        /// <param name="columns">Columnas a incluir en el resultado.</param>
        /// <returns>DataTable con filas pendientes de sincronización.</returns>
        public DataTable GetUnsyncedTableData(string tableName, List<string> columns)
        {
            // Asegurar conexión abierta
            OpenConnection();

            // Obtener columnas existentes para validar si existe 'IsSynced'
            var existingColumns = GetColumnNames(tableName);

            string columnsPart = columns != null && columns.Count > 0
                ? string.Join(", ", columns.ConvertAll(c => EscapeName(c)))
                : "*";

            string query;

            if (existingColumns.Contains("IsSynced"))
            {
                // Solo filas donde IsSynced sea 0 (no sincronizado)
                query = $"SELECT {columnsPart} FROM {EscapeName(tableName)} WHERE IsSynced = 0";
            }
            else
            {
                // Si no existe columna IsSynced, devolver todas las filas
                query = $"SELECT {columnsPart} FROM {EscapeName(tableName)}";
            }

            var dt = new DataTable();

            using var command = new MySqlCommand(query, _connection);
            using var adapter = new MySqlDataAdapter(command);
            adapter.Fill(dt);

            return dt;
        }

        /// <summary>
        /// Marca las filas proporcionadas como sincronizadas (IsSynced = 1).
        /// Solo funciona si la tabla tiene la columna 'IsSynced'.
        /// </summary>
        /// <param name="tableName">Nombre de la tabla.</param>
        /// <param name="data">Filas que se marcarán como sincronizadas.</param>
        public void MarkRowsAsSynced(string tableName, DataTable data)
        {
            // Si no hay datos, no hacer nada
            if (data == null || data.Rows.Count == 0) return;

            // Asegurar conexión abierta
            OpenConnection();

            // Verificar si la columna 'IsSynced' existe en la tabla
            var existingColumns = GetColumnNames(tableName);
            if (!existingColumns.Contains("IsSynced"))
                return;  // No existe columna, no se puede marcar

            // Actualizar fila por fila el estado de sincronización
            foreach (DataRow row in data.Rows)
            {
                var id = row["Id"]; // Suponemos que la clave primaria es 'Id'

                string query = $"UPDATE {EscapeName(tableName)} SET IsSynced = 1 WHERE Id = @id";

                using var command = new MySqlCommand(query, _connection);
                command.Parameters.AddWithValue("@id", id);

                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Libera recursos y cierra la conexión si está abierta.
        /// </summary>
        public void Dispose()
        {
            CloseConnection();
            _connection?.Dispose();
        }
    }
}
