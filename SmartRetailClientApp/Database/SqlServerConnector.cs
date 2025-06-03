using System.Data;
using System.Data.SqlClient;

namespace SmartRetailClientApp.Database
{
    /// <summary>
    /// Conector para base de datos SQL Server.
    /// Implementa métodos para gestión de conexión, obtención de tablas/columnas, 
    /// consulta de datos y sincronización de filas mediante columna IsSynced.
    /// </summary>
    public class SqlServerConnector : IDbConnector, IDisposable
    {
        private readonly string _connectionString;
        private SqlConnection _connection;

        /// <summary>
        /// Constructor que recibe cadena de conexión completa.
        /// </summary>
        /// <param name="connectionString">Cadena de conexión a SQL Server.</param>
        public SqlServerConnector(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Escapa nombres de tablas o columnas para SQL Server usando corchetes.
        /// Reemplaza corchetes cerrados internos por dobles para evitar errores.
        /// </summary>
        /// <param name="name">Nombre a escapar.</param>
        /// <returns>Nombre escapado entre corchetes.</returns>
        private string EscapeName(string name) => $"[{name.Replace("]", "]]")}]";

        /// <summary>
        /// Abre la conexión a la base de datos si no está abierta.
        /// </summary>
        public void OpenConnection()
        {
            if (_connection == null)
                _connection = new SqlConnection(_connectionString);

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
        /// Obtiene los nombres de todas las tablas base definidas en la base de datos actual.
        /// </summary>
        /// <returns>Lista con nombres de tablas.</returns>
        public List<string> GetTableNames()
        {
            var tables = new List<string>();

            OpenConnection();

            // Consulta a INFORMATION_SCHEMA para obtener tablas base del catálogo actual
            var query = @"SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_CATALOG = DB_NAME();";

            using (var command = new SqlCommand(query, _connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    tables.Add(reader.GetString(0));
                }
            }

            return tables;
        }

        /// <summary>
        /// Obtiene los nombres de columnas de una tabla específica, ordenados por su posición.
        /// </summary>
        /// <param name="tableName">Nombre de la tabla.</param>
        /// <returns>Lista con nombres de columnas.</returns>
        public List<string> GetColumnNames(string tableName)
        {
            var columns = new List<string>();

            OpenConnection();

            // Consulta a INFORMATION_SCHEMA para obtener columnas de la tabla indicada
            var query = @"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName ORDER BY ORDINAL_POSITION;";

            using (var command = new SqlCommand(query, _connection))
            {
                command.Parameters.AddWithValue("@tableName", tableName);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        columns.Add(reader.GetString(0));
                    }
                }
            }

            return columns;
        }

        /// <summary>
        /// Obtiene datos de una tabla, con las columnas indicadas o todas si no se especifican.
        /// </summary>
        /// <param name="tableName">Nombre de la tabla.</param>
        /// <param name="columns">Columnas a seleccionar.</param>
        /// <returns>DataTable con los datos consultados.</returns>
        public DataTable GetTableData(string tableName, List<string> columns)
        {
            var dt = new DataTable();

            OpenConnection();

            // Construir lista de columnas escapadas o seleccionar todas
            string columnsPart = columns != null && columns.Count > 0
                ? string.Join(", ", columns.ConvertAll(c => EscapeName(c)))
                : "*";

            string query = $"SELECT {columnsPart} FROM {EscapeName(tableName)}";

            using (var command = new SqlCommand(query, _connection))
            using (var adapter = new SqlDataAdapter(command))
            {
                adapter.Fill(dt);
            }

            return dt;
        }

        /// <summary>
        /// Obtiene filas no sincronizadas (IsSynced = 0) si la columna existe, o todas las filas si no.
        /// </summary>
        /// <param name="tableName">Nombre de la tabla.</param>
        /// <param name="columns">Columnas a incluir en el resultado.</param>
        /// <returns>DataTable con filas pendientes de sincronización.</returns>
        public DataTable GetUnsyncedTableData(string tableName, List<string> columns)
        {
            OpenConnection();

            var existingColumns = GetColumnNames(tableName);

            string columnsPart = columns != null && columns.Count > 0
                ? string.Join(", ", columns.ConvertAll(c => EscapeName(c)))
                : "*";

            string query;

            if (existingColumns.Contains("IsSynced"))
            {
                // Filtrar por filas donde IsSynced = 0
                query = $"SELECT {columnsPart} FROM {EscapeName(tableName)} WHERE IsSynced = 0";
            }
            else
            {
                // Si no existe columna IsSynced, devolver todas las filas
                query = $"SELECT {columnsPart} FROM {EscapeName(tableName)}";
            }

            var dt = new DataTable();

            using var command = new SqlCommand(query, _connection);
            using var adapter = new SqlDataAdapter(command);
            adapter.Fill(dt);

            return dt;
        }

        /// <summary>
        /// Marca las filas indicadas en la tabla como sincronizadas (IsSynced = 1).
        /// </summary>
        /// <param name="tableName">Nombre de la tabla.</param>
        /// <param name="data">Filas a marcar.</param>
        public void MarkRowsAsSynced(string tableName, DataTable data)
        {
            if (data == null || data.Rows.Count == 0) return;

            OpenConnection();

            var existingColumns = GetColumnNames(tableName);
            if (!existingColumns.Contains("IsSynced"))
                return;

            // Actualizar fila por fila el estado IsSynced
            foreach (DataRow row in data.Rows)
            {
                var id = row["Id"]; // Se asume que existe columna Id

                string query = $"UPDATE {EscapeName(tableName)} SET IsSynced = 1 WHERE Id = @id";

                using var command = new SqlCommand(query, _connection);
                command.Parameters.AddWithValue("@id", id);

                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Cierra la conexión y libera recursos.
        /// </summary>
        public void Dispose()
        {
            CloseConnection();
            _connection?.Dispose();
        }
    }
}
