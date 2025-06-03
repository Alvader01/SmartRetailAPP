using System.Data;
using System.Data.SQLite;

namespace SmartRetailClientApp.Database
{
    /// <summary>
    /// Implementación del conector para bases de datos SQLite.
    /// Proporciona métodos para abrir/cerrar conexión, obtener tablas, columnas y manejar sincronización.
    /// </summary>
    public class SQLiteConnector : IDbConnector, IDisposable
    {
        private readonly string _connectionString;
        private SQLiteConnection _connection;

        /// <summary>
        /// Constructor que inicializa el conector con la ruta al archivo SQLite.
        /// </summary>
        /// <param name="databaseFilePath">Ruta del archivo de base de datos SQLite.</param>
        public SQLiteConnector(string databaseFilePath)
        {
            _connectionString = $"Data Source={databaseFilePath};Version=3;";
        }

        /// <summary>
        /// Escapa nombres de tablas o columnas para evitar problemas con caracteres reservados.
        /// En SQLite se usan dobles comillas.
        /// </summary>
        /// <param name="name">Nombre a escapar.</param>
        /// <returns>Nombre escapado con dobles comillas.</returns>
        private string EscapeName(string name) => $"\"{name.Replace("\"", "\"\"")}\"";

        /// <summary>
        /// Abre la conexión a la base de datos SQLite si no está ya abierta.
        /// </summary>
        public void OpenConnection()
        {
            if (_connection == null)
                _connection = new SQLiteConnection(_connectionString);

            if (_connection.State != ConnectionState.Open)
                _connection.Open();
        }

        /// <summary>
        /// Cierra la conexión si está abierta.
        /// </summary>
        public void CloseConnection()
        {
            if (_connection != null && _connection.State != ConnectionState.Closed)
                _connection.Close();
        }

        /// <summary>
        /// Obtiene la lista de nombres de todas las tablas disponibles (excluye tablas del sistema SQLite).
        /// </summary>
        /// <returns>Lista de nombres de tablas.</returns>
        public List<string> GetTableNames()
        {
            var tables = new List<string>();

            // Asegurar conexión abierta
            OpenConnection();

            // Consulta para obtener tablas definidas por el usuario
            var query = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";

            using (var command = new SQLiteCommand(query, _connection))
            using (var reader = command.ExecuteReader())
            {
                // Leer y agregar cada nombre de tabla
                while (reader.Read())
                {
                    tables.Add(reader.GetString(0));
                }
            }

            return tables;
        }

        /// <summary>
        /// Obtiene los nombres de columnas de una tabla específica usando PRAGMA table_info.
        /// </summary>
        /// <param name="tableName">Nombre de la tabla.</param>
        /// <returns>Lista con nombres de columnas.</returns>
        public List<string> GetColumnNames(string tableName)
        {
            var columns = new List<string>();

            // Asegurar conexión abierta
            OpenConnection();

            // PRAGMA para obtener información de columnas de la tabla
            var query = $"PRAGMA table_info({EscapeName(tableName)});";

            using (var command = new SQLiteCommand(query, _connection))
            using (var reader = command.ExecuteReader())
            {
                // Leer el nombre de cada columna (columna "name" en el resultado)
                while (reader.Read())
                {
                    columns.Add(reader.GetString(reader.GetOrdinal("name")));
                }
            }

            return columns;
        }

        /// <summary>
        /// Obtiene los datos de una tabla con las columnas indicadas.
        /// Si no se especifican columnas, se obtienen todas.
        /// </summary>
        /// <param name="tableName">Nombre de la tabla.</param>
        /// <param name="columns">Lista de columnas a incluir.</param>
        /// <returns>DataTable con los datos solicitados.</returns>
        public DataTable GetTableData(string tableName, List<string> columns)
        {
            var dt = new DataTable();

            // Asegurar conexión abierta
            OpenConnection();

            // Construir lista de columnas escapadas o "*"
            string columnsPart = columns != null && columns.Count > 0
                ? string.Join(", ", columns.ConvertAll(c => EscapeName(c)))
                : "*";

            string query = $"SELECT {columnsPart} FROM {EscapeName(tableName)}";

            using (var command = new SQLiteCommand(query, _connection))
            using (var adapter = new SQLiteDataAdapter(command))
            {
                // Ejecutar consulta y llenar DataTable
                adapter.Fill(dt);
            }

            return dt;
        }

        /// <summary>
        /// Obtiene filas no sincronizadas según columna 'IsSynced' (valor 0).
        /// Si no existe la columna, devuelve todas las filas.
        /// </summary>
        /// <param name="tableName">Nombre de la tabla.</param>
        /// <param name="columns">Columnas a incluir en el resultado.</param>
        /// <returns>DataTable con filas pendientes de sincronización.</returns>
        public DataTable GetUnsyncedTableData(string tableName, List<string> columns)
        {
            OpenConnection();

            // Obtener columnas para verificar existencia de IsSynced
            var existingColumns = GetColumnNames(tableName);

            // Construir lista de columnas o "*"
            string columnsPart = columns != null && columns.Count > 0
                ? string.Join(", ", columns.ConvertAll(c => EscapeName(c)))
                : "*";

            string query;

            if (existingColumns.Contains("IsSynced"))
            {
                // Filtrar filas con IsSynced = 0 (no sincronizadas)
                query = $"SELECT {columnsPart} FROM {EscapeName(tableName)} WHERE IsSynced = 0";
            }
            else
            {
                // No hay columna IsSynced, devolver todas las filas
                query = $"SELECT {columnsPart} FROM {EscapeName(tableName)}";
            }

            var dt = new DataTable();

            using var command = new SQLiteCommand(query, _connection);
            using var adapter = new SQLiteDataAdapter(command);
            adapter.Fill(dt);

            return dt;
        }

        /// <summary>
        /// Marca como sincronizadas las filas especificadas en la tabla estableciendo 'IsSynced' a 1.
        /// </summary>
        /// <param name="tableName">Nombre de la tabla.</param>
        /// <param name="data">Filas a marcar como sincronizadas.</param>
        public void MarkRowsAsSynced(string tableName, DataTable data)
        {
            // Si no hay filas para procesar, salir
            if (data == null || data.Rows.Count == 0) return;

            OpenConnection();

            // Verificar existencia de columna IsSynced
            var existingColumns = GetColumnNames(tableName);
            if (!existingColumns.Contains("IsSynced"))
                return;

            // Actualizar fila por fila marcando IsSynced = 1
            foreach (DataRow row in data.Rows)
            {
                var id = row["Id"]; // Se asume que existe columna Id como clave primaria

                string query = $"UPDATE {EscapeName(tableName)} SET IsSynced = 1 WHERE Id = @id";

                using var command = new SQLiteCommand(query, _connection);
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
