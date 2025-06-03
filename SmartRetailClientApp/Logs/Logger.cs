using System;
using System.IO;

namespace SmartRetailClientApp.Logging
{
    /// <summary>
    /// Clase estática para manejo sencillo de logs de texto en archivo local.
    /// </summary>
    public static class Logger
    {
        // Directorio donde se almacenarán los logs
        private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppLogs");

        // Ruta completa del archivo de log
        private static readonly string LogFilePath = Path.Combine(LogDirectory, "sync_log.txt");

        // Constructor estático para asegurar que el directorio de logs exista
        static Logger()
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                    Directory.CreateDirectory(LogDirectory);
            }
            catch
            {
                // Ignorar errores en creación de carpeta para evitar romper la aplicación
            }
        }

        /// <summary>
        /// Escribe un mensaje en el archivo de log, prefijado con la fecha y hora actuales.
        /// En caso de error en escritura, los errores se silencian para no afectar la app.
        /// </summary>
        /// <param name="message">Mensaje a registrar</param>
        public static void WriteLog(string message)
        {
            try
            {
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
                File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
            }
            catch
            {
                // Silenciar errores al escribir en log para evitar interrupciones
            }
        }

        /// <summary>
        /// Lee todo el contenido actual del archivo de log.
        /// Si el archivo no existe, devuelve un mensaje indicando ausencia de registros.
        /// </summary>
        /// <returns>Contenido completo del log como string o mensaje alternativo.</returns>
        public static string ReadLog()
        {
            return File.Exists(LogFilePath)
                ? File.ReadAllText(LogFilePath)
                : "No hay registros todavía.";
        }
    }
}
