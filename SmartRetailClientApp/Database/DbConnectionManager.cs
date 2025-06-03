using System.IO;
using SmartRetailClientApp.Database;
using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using Npgsql;
using System.Data.SQLite;

namespace SmartRetailClientApp
{
    /// <summary>
    /// Clase estática encargada de gestionar la conexión con bases de datos
    /// soportando múltiples motores: SQLite, SQL Server, MySQL y PostgreSQL.
    /// </summary>
    public static class DbConnectionManager
    {
        /// <summary>
        /// Intenta conectar con la base de datos según los parámetros proporcionados.
        /// Detecta el tipo de base de datos (SQLite o servidor) y prueba conexiones
        /// hasta encontrar una válida o lanzar excepción si ninguna funciona.
        /// </summary>
        /// <param name="host">Host o ruta del archivo de base de datos</param>
        /// <param name="database">Nombre de la base de datos (para servidores)</param>
        /// <param name="user">Usuario para autenticación</param>
        /// <param name="pass">Contraseña para autenticación</param>
        /// <param name="useIntegratedSecurity">Indica si usar seguridad integrada (Windows Auth)</param>
        /// <returns>IDbConnector con conexión abierta válida</returns>
        public static IDbConnector TryConnect(string host, string database, string user, string pass, bool useIntegratedSecurity)
        {
            // Detectar si el host apunta a un archivo SQLite por extensión
            bool isSQLite = host.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase)
                            || host.EndsWith(".db", StringComparison.OrdinalIgnoreCase);

            if (isSQLite)
            {
                // Manejo especial para SQLite, que es archivo local
                if (!File.Exists(host))
                    throw new FileNotFoundException($"Archivo SQLite no encontrado: {host}");

                var connector = new SQLiteConnector(host);
                try
                {
                    // Abrir conexión y verificar que existan tablas
                    connector.OpenConnection();
                    var tables = connector.GetTableNames();

                    if (tables.Count > 0)
                        return connector;

                    // Si no hay tablas, cerrar conexión y lanzar excepción
                    connector.Dispose();
                    throw new Exception("No se encontraron tablas en la base de datos SQLite.");
                }
                catch (SQLiteException ex)
                {
                    // En caso de error SQLite, limpiar y relanzar con mensaje detallado
                    connector.Dispose();
                    throw new SQLiteException($"Error SQLite: {ex.Message}", ex);
                }
                catch
                {
                    // Para cualquier otra excepción, limpiar y relanzar
                    connector.Dispose();
                    throw;
                }
            }
            else
            {
                // Para bases de datos en servidor, probar varios tipos de conexión

                var connectorFactories = new List<Func<IDbConnector>>
                {
                    // SQL Server
                    () => {
                        string connStr = useIntegratedSecurity
                            ? $"Server={host};Database={database};Integrated Security=True;"
                            : $"Server={host};Database={database};User Id={user};Password={pass};";
                        return new SqlServerConnector(connStr);
                    },
                    // MySQL
                    () => new MySqlConnector(host, database, user, pass),
                    // PostgreSQL
                    () => new PostgreSqlConnector(host, database, user, pass)
                };

                List<Exception> exceptions = new();

                foreach (var factory in connectorFactories)
                {
                    IDbConnector? connector = null;
                    try
                    {
                        // Crear conector, abrir conexión y validar tablas existentes
                        connector = factory();
                        connector.OpenConnection();
                        var tables = connector.GetTableNames();

                        if (tables.Count > 0)
                            return connector;

                        // Si no hay tablas, liberar recursos y seguir al siguiente conector
                        connector.Dispose();
                    }
                    catch (SqlException ex)
                    {
                        exceptions.Add(ex);
                        connector?.Dispose();
                    }
                    catch (MySqlException ex)
                    {
                        exceptions.Add(ex);
                        connector?.Dispose();
                    }
                    catch (Npgsql.PostgresException ex)
                    {
                        exceptions.Add(ex);
                        connector?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                        connector?.Dispose();
                    }
                }

                // Si hubo excepciones durante los intentos, lanzar la primera para referencia
                if (exceptions.Count > 0)
                    throw exceptions[0];

                // Si ningún conector pudo establecer conexión válida, lanzar excepción genérica
                throw new Exception("No se pudo conectar a ninguna base de datos con las credenciales proporcionadas.");
            }
        }
    }
}
