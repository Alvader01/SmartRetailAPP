using System.Data;
using System.Globalization;
using System.Text;

namespace SmartRetailClientApp.Transformations
{
    public static class DataTransformer
    {
        // Mapas de columnas por tabla para convertir nombres de columna DB a nombres JSON API
        private static readonly Dictionary<string, Dictionary<string, string>> TableColumnMappings = new()
        {
            {
                "producto", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Producto_id", "productoId" },
                    { "TiendaId", "tiendaId" },
                    { "Nombre", "nombre" },
                    { "Precio", "precio" },
                    { "Stock", "stock" }
                }
            },
            {
                "cliente", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Cliente_id", "clienteId" },
                    { "TiendaId", "tiendaId" },
                    { "Nombre", "nombre" },
                    { "Correo", "correo" },
                    { "Telefono", "telefono" }
                }
            },
            {
                "venta", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Venta_id", "ventaId" },
                    { "TiendaId", "tiendaId" },
                    { "Fecha", "fecha" },
                    { "Total", "total" },
                    { "Cliente_id", "clienteId" }
                }
            },
            {
                "detalle_venta", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Venta_id", "ventaId" },
                    { "Producto_id", "productoId" },
                    { "TiendaId", "tiendaId" },
                    { "Cantidad", "cantidad" },
                    { "Subtotal", "subtotal" }
                }
            }
        };

        /// <summary>
        /// Transforma un DataTable original en una lista de diccionarios con nombres de columna mapeados y valores formateados.
        /// También elimina filas duplicadas exactas basándose en el contenido.
        /// </summary>
        /// <param name="original">DataTable con los datos originales</param>
        /// <param name="tableName">Nombre de la tabla para buscar mapeos de columnas</param>
        /// <returns>Lista de diccionarios con claves y valores listos para serializar JSON</returns>
        public static List<Dictionary<string, object>> Transform(DataTable original, string tableName)
        {
            var seenRows = new HashSet<string>(); // Para evitar duplicados exactos
            var transformedList = new List<Dictionary<string, object>>();

            // Obtener el diccionario de mapeo de columnas para esta tabla (o null si no existe)
            TableColumnMappings.TryGetValue(tableName.ToLower(), out var columnMap);

            foreach (DataRow row in original.Rows)
            {
                // Crear una clave única para la fila concatenando todas sus columnas en minúsculas
                var keyBuilder = new StringBuilder();
                foreach (DataColumn col in original.Columns)
                {
                    var val = row[col]?.ToString().Trim().ToLower() ?? "";
                    keyBuilder.Append(val).Append("|");
                }

                string key = keyBuilder.ToString();

                // Si ya vimos esta fila (duplicada), la saltamos
                if (seenRows.Contains(key))
                    continue;

                seenRows.Add(key);

                // Construir el diccionario para la fila actual con los nombres JSON y valores formateados
                var dict = new Dictionary<string, object>();

                foreach (DataColumn col in original.Columns)
                {
                    // Usar el nombre mapeado si existe, sino el nombre original de la columna
                    string jsonKey = col.ColumnName;
                    if (columnMap != null && columnMap.TryGetValue(col.ColumnName, out var mappedName))
                        jsonKey = mappedName;

                    if (col.DataType == typeof(string))
                    {
                        // Trim para strings
                        dict[jsonKey] = row[col]?.ToString().Trim();
                    }
                    else if (col.DataType == typeof(DateTime))
                    {
                        // Formatear fechas en formato ISO 8601 si no es DBNull
                        if (row[col] != DBNull.Value)
                            dict[jsonKey] = ((DateTime)row[col]).ToString("o", CultureInfo.InvariantCulture);
                        else
                            dict[jsonKey] = null;
                    }
                    else
                    {
                        // Para otros tipos, asignar el valor o null si es DBNull
                        dict[jsonKey] = row[col] == DBNull.Value ? null : row[col];
                    }
                }

                transformedList.Add(dict);
            }

            return transformedList;
        }
    }
}
