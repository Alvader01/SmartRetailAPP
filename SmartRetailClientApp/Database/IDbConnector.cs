using System.Collections.Generic;
using System.Data;

namespace SmartRetailClientApp.Database
{
    /// <summary>
    /// Interfaz que define las operaciones básicas para un conector de base de datos.
    /// Permite abrir y cerrar conexiones, obtener metadatos y manipular datos de tablas.
    /// </summary>
    public interface IDbConnector : IDisposable
    {
        /// <summary>
        /// Abre la conexión a la base de datos.
        /// Implementación debe establecer la conexión física y dejarla lista para uso.
        /// </summary>
        void OpenConnection();

        /// <summary>
        /// Cierra la conexión abierta a la base de datos.
        /// Implementación debe liberar recursos y cerrar cualquier conexión activa.
        /// </summary>
        void CloseConnection();

        /// <summary>
        /// Obtiene la lista de nombres de todas las tablas disponibles en la base de datos.
        /// Útil para exploración dinámica de esquema o validaciones.
        /// </summary>
        /// <returns>Lista con nombres de tablas existentes.</returns>
        List<string> GetTableNames();

        /// <summary>
        /// Obtiene la lista de nombres de columnas de una tabla específica.
        /// Permite conocer la estructura de la tabla para consultas o transformaciones.
        /// </summary>
        /// <param name="tableName">Nombre de la tabla de interés.</param>
        /// <returns>Lista con nombres de columnas.</returns>
        List<string> GetColumnNames(string tableName);

        /// <summary>
        /// Obtiene los datos de una tabla, incluyendo solo las columnas indicadas.
        /// Puede usarse para consultas específicas o para preparar datos para sincronización.
        /// </summary>
        /// <param name="tableName">Nombre de la tabla a consultar.</param>
        /// <param name="columns">Lista de columnas que se desean obtener.</param>
        /// <returns>Un <see cref="DataTable"/> con las filas y columnas solicitadas.</returns>
        DataTable GetTableData(string tableName, List<string> columns);

        /// <summary>
        /// Obtiene únicamente las filas de la tabla que no han sido sincronizadas aún.
        /// Se asume que existe una columna booleana 'Sincronizado' que indica estado.
        /// </summary>
        /// <param name="tableName">Nombre de la tabla a consultar.</param>
        /// <param name="columns">Lista de columnas que se desean obtener.</param>
        /// <returns><see cref="DataTable"/> con filas pendientes de sincronización.</returns>
        DataTable GetUnsyncedTableData(string tableName, List<string> columns);

        /// <summary>
        /// Marca las filas especificadas como sincronizadas, usualmente actualizando una columna 'Sincronizado'.
        /// Esto evita que las mismas filas se vuelvan a sincronizar en procesos futuros.
        /// </summary>
        /// <param name="tableName">Nombre de la tabla que contiene las filas.</param>
        /// <param name="rows">Tabla con las filas que se marcarán como sincronizadas.</param>
        void MarkRowsAsSynced(string tableName, DataTable rows);
    }
}
