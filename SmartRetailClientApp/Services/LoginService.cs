using System.IO;
using System.Net;
using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using Npgsql;
using System.Data.SQLite;
using SmartRetailClientApp.Database;

namespace SmartRetailClientApp.Services
{
    /// <summary>
    /// Servicio para validar datos de conexión y crear conexiones a diferentes bases de datos.
    /// Soporta SQLite, SQL Server, MySQL y PostgreSQL.
    /// </summary>
    public class LoginService
    {
        /// <summary>
        /// Valida los parámetros de entrada para la conexión.
        /// </summary>
        /// <param name="host">Host o archivo de base de datos (para SQLite).</param>
        /// <param name="database">Nombre de la base de datos (no requerido para SQLite).</param>
        /// <param name="user">Usuario para la conexión (no requerido si se usa seguridad integrada o SQLite).</param>
        /// <param name="useIntegratedSecurity">Indica si se usa seguridad integrada para SQL Server.</param>
        /// <returns>Tupla con booleano indicando validez y mensaje de error (vacío si es válido).</returns>
        public (bool IsValid, string Message) ValidateInputs(string host, string database, string user, bool useIntegratedSecurity)
        {
            bool isSQLite = host.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(host))
                return (false, "Por favor, ingrese el host o archivo de base de datos.");

            if (!ValidateHost(host, isSQLite))
                return (false, "Host inválido. Debe ser una IP, nombre DNS válido, nombre de instancia SQL Server (ejemplo: .\\SQLEXPRESS) o ruta a archivo SQLite.");

            if (string.IsNullOrWhiteSpace(database) && !isSQLite)
                return (false, "Por favor, ingrese el nombre de la base de datos.");

            if (!useIntegratedSecurity && !isSQLite)
            {
                if (string.IsNullOrWhiteSpace(user))
                    return (false, "Por favor, ingrese el usuario.");

                if (!ValidateUserOrDbName(user))
                    return (false, "Usuario inválido. No debe contener caracteres especiales.");
            }

            if (!isSQLite && !ValidateUserOrDbName(database))
                return (false, "Nombre de base de datos inválido. No debe contener caracteres especiales.");

            if (isSQLite)
            {
                if (!File.Exists(host))
                    return (false, "El archivo SQLite no existe.");

                if (IsFileLocked(host))
                    return (false, "El archivo SQLite está en uso por otro proceso.");
            }

            return (true, "");
        }

        /// <summary>
        /// Intenta crear una conexión de base de datos usando DbConnectionManager.
        /// </summary>
        /// <param name="host">Host o archivo SQLite.</param>
        /// <param name="database">Nombre de la base de datos.</param>
        /// <param name="user">Usuario de la base de datos.</param>
        /// <param name="password">Contraseña del usuario.</param>
        /// <param name="useIntegratedSecurity">Indica si se usa seguridad integrada.</param>
        /// <returns>Conector a base de datos implementando IDbConnector.</returns>
        public IDbConnector Connect(string host, string database, string user, string password, bool useIntegratedSecurity)
        {
            bool isSQLite = host.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase);

            if (isSQLite)
            {
                // Para SQLite, host = ruta al archivo, database vacío
                return DbConnectionManager.TryConnect(host, "", user, password, useIntegratedSecurity);
            }
            else
            {
                // Para otros, host y database normales
                return DbConnectionManager.TryConnect(host, database, user, password, useIntegratedSecurity);
            }
        }

        private bool ValidateHost(string host, bool isSQLite)
        {
            if (isSQLite)
                return true;

            if (IPAddress.TryParse(host, out _))
                return true;

            if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                return true;

            // Soporte para nombre de instancia SQL Server (ejemplo: servidor\instancia)
            if (host.Contains('\\'))
            {
                var parts = host.Split('\\');
                if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0].Trim()) && !string.IsNullOrWhiteSpace(parts[1].Trim()))
                {
                    if (parts[0].Trim() == ".")
                        return ValidateInstanceName(parts[1].Trim());

                    return ValidateDnsOrIp(parts[0].Trim()) && ValidateInstanceName(parts[1].Trim());
                }
                return false;
            }

            return ValidateDnsOrIp(host);
        }

        private bool ValidateDnsOrIp(string hostPart)
        {
            if (hostPart == ".")
                return true;

            var hostParts = hostPart.Split('.');
            foreach (var part in hostParts)
            {
                if (string.IsNullOrWhiteSpace(part) || part.Length > 63)
                    return false;

                foreach (char c in part)
                {
                    if (!char.IsLetterOrDigit(c) && c != '-')
                        return false;
                }
            }
            return true;
        }

        private bool ValidateInstanceName(string instance)
        {
            foreach (char c in instance)
            {
                if (!(char.IsLetterOrDigit(c) || c == '-' || c == '_'))
                    return false;
            }
            return true;
        }

        private bool ValidateUserOrDbName(string input)
        {
            foreach (char c in input)
            {
                if (!(char.IsLetterOrDigit(c) || c == '_'))
                    return false;
            }
            return true;
        }

        private bool IsFileLocked(string path)
        {
            try
            {
                using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    return false;
                }
            }
            catch (IOException)
            {
                return true;
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Devuelve un mensaje amigable para excepciones comunes de bases de datos.
        /// </summary>
        /// <param name="ex">Excepción capturada.</param>
        /// <returns>Mensaje de error amigable para mostrar al usuario.</returns>
        public string GetFriendlyErrorMessage(Exception ex)
        {
            return ex switch
            {
                SqlException sqlEx => $"Error SQL Server: {sqlEx.Message}",
                MySqlException mySqlEx => $"Error MySQL: {mySqlEx.Message}",
                PostgresException pgEx => $"Error PostgreSQL: {pgEx.Message}",
                SQLiteException sqliteEx => $"Error SQLite: {sqliteEx.Message}",
                _ => ex.Message,
            };
        }
    }
}
