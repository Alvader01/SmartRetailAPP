using System.Data;
using Npgsql;

namespace SmartRetailClientApp.Database
{
    /// <summary>
    /// Implementación del conector para bases de datos PostgreSQL.
    /// Proporciona métodos para abrir/cerrar conexión, obtener tablas, columnas y manejar sincronización.
    /// </summary>
    public class PostgreSqlConnector : IDbConnector, IDisposable
    {
        private readonly string _connectionString;
        private NpgsqlConnection _connection;

        /// <summary>
        /// Constructor que inicializa el conector con parámetros de conexión.
        /// </summary>
        /// <param name="host">Host o IP del servidor PostgreSQL.</param>
        /// <param name="database">Nombre de la base de datos.</param>
        /// <param name="user">Usuario para la conexión.</param>
        /// <param name="password">Contraseña del usuario.</param>
        /// <param name="port">Puerto del servidor, por defecto 5432.</param>
        public PostgreSqlConnector(string host, string database, string user, string password, int port = 5432)
        {
            _connectionString = $"Host={host};Port={port};Database={database};Username={user};Password={password};";
        }

        /// <summary>
        /// Escapa nombres de tablas o columnas para evitar problemas con caracteres reservados.
        /// En PostgreSQL se usan dobles comillas.
        /// </summary>
        /// <param name="name">Nombre a escapar.</param>
        /// <returns>Nombre escapado con dobles comillas.</returns>
        private string EscapeName(string name) => $"\"{name.Replace("\"", "\"\"")}\"";

        /// <summary>
        /// Abre la conexión a la base de datos PostgreSQL si no está ya abierta.
        /// </summary>
        public void OpenConnection()
        {
            if (_connection == null)
                _connection = new NpgsqlConnection(_connectionString);

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
        /// Obtiene la lista de nombres de todas las tablas disponibles en el esquema público.
        /// </summary>
        /// <returns>Lista de nombres de tablas.</returns>
        public List<string> GetTableNames()
        {
            var tables = new List<string>();

            // Aseguramos conexión abierta
            OpenConnection();

            // Consultar tablas en el esquema público y de tipo base table
            var query = "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_type='BASE TABLE';";

            using (var command = new NpgsqlCommand(query, _connection))
            using (var reader = command.ExecuteReader())
            {
                // Leer cada tabla y añadir a la lista
                while (reader.Read())
                {
                    tables.Add(reader.GetString(0));
                }
            }

            return tables;
        }

        /// <summary>
        /// Obtiene los nombres de columnas de una tabla específica en el esquema público.
        /// </summary>
        /// <param name="tableName">Nombre de la tabla.</param>
        /// <returns>Lista con nombres de columnas ordenados.</returns>
        public List<string> GetColumnNames(string tableName)
        {
            var columns = new List<string>();

            // Aseguramos conexión abierta
            OpenConnection();

            // Consulta parametrizada para evitar inyección SQL
            var query = @"SELECT column_name FROM information_schema.columns 
                          WHERE table_name = @tableName AND table_schema = 'public' ORDER BY ordinal_position;";

            using (var command = new NpgsqlCommand(query, _connection))
            {
                command.Parameters.AddWithValue("tableName", tableName);

                using (var reader = command.ExecuteReader())
                {
                    // Leer y agregar cada columna
                    while (reader.Read())
                    {
                        columns.Add(reader.GetString(0));
                    }
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

            // Construir la parte de columnas escapadas o "*"
            string columnsPart = columns != null && columns.Count > 0
                ? string.Join(", ", columns.ConvertAll(c => EscapeName(c)))
                : "*";

            string query = $"SELECT {columnsPart} FROM {EscapeName(tableName)}";

            using (var command = new NpgsqlCommand(query, _connection))
            using (var adapter = new NpgsqlDataAdapter(command))
            {
                // Ejecutar la consulta y llenar DataTable
                adapter.Fill(dt);
            }

            return dt;
        }

        /// <summary>
        /// Obtiene filas no sincronizadas según columna 'IsSynced' (booleano o 0/false).
        /// Si no existe esa columna, devuelve todas las filas.
        /// </summary>
        /// <param name="tableName">Nombre de la tabla.</param>
        /// <param name="columns">Columnas a incluir en el resultado.</param>
        /// <returns>DataTable con filas pendientes de sincronización.</returns>
        public DataTable GetUnsyncedTableData(string tableName, List<string> columns)
        {
            OpenConnection();

            // Obtener columnas existentes para determinar si existe IsSynced
            var existingColumns = GetColumnNames(tableName);

            string columnsPart = columns != null && columns.Count > 0
                ? string.Join(", ", columns.ConvertAll(c => EscapeName(c)))
                : "*";

            string query;

            if (existingColumns.Contains("IsSynced"))
            {
                // Filtrar filas no sincronizadas (IsSynced = false o 0)
                query = $"SELECT {columnsPart} FROM {EscapeName(tableName)} WHERE \"IsSynced\" = false OR \"IsSynced\" = 0";
            }
            else
            {
                // Si no existe la columna, devolver todas las filas
                query = $"SELECT {columnsPart} FROM {EscapeName(tableName)}";
            }

            var dt = new DataTable();

            using var command = new NpgsqlCommand(query, _connection);
            using var adapter = new NpgsqlDataAdapter(command);
            adapter.Fill(dt);

            return dt;
        }

        /// <summary>
        /// Marca como sincronizadas las filas especificadas en la tabla, estableciendo 'IsSynced' a true.
        /// </summary>
        /// <param name="tableName">Nombre de la tabla.</param>
        /// <param name="data">Filas a marcar como sincronizadas.</param>
        public void MarkRowsAsSynced(string tableName, DataTable data)
        {
            // Si no hay datos, salir
            if (data == null || data.Rows.Count == 0) return;

            OpenConnection();

            // Verificar si la columna IsSynced existe
            var existingColumns = GetColumnNames(tableName);
            if (!existingColumns.Contains("IsSynced"))
                return;

            // Actualizar cada fila: establecer IsSynced = true para la fila con Id específico
            foreach (DataRow row in data.Rows)
            {
                var id = row["Id"]; // Se asume que la clave primaria es "Id"

                string query = $"UPDATE {EscapeName(tableName)} SET \"IsSynced\" = true WHERE \"Id\" = @id";

                using var command = new NpgsqlCommand(query, _connection);
                command.Parameters.AddWithValue("id", id);

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
