using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using SmartRetailClientApp.Database;

namespace SmartRetailClientApp.Tests
{
    public static class TestConnectors
    {
        private static string FormatTableData(DataTable data, int maxRows = 5)
        {
            if (data.Rows.Count == 0)
                return "(sin datos)";

            var lines = new List<string>();

            for (int i = 0; i < Math.Min(data.Rows.Count, maxRows); i++)
            {
                var row = data.Rows[i];
                var values = new List<string>();

                foreach (var item in row.ItemArray)
                {
                    values.Add(item?.ToString() ?? "NULL");
                }

                lines.Add($"Fila {i + 1}: {string.Join(" | ", values)}");
            }

            if (data.Rows.Count > maxRows)
                lines.Add($"... ({data.Rows.Count - maxRows} filas más)");

            return string.Join("\n", lines);
        }

        public static void TestMySql(string host, string db, string user, string pass)
        {
            try
            {
                using var dbConn = new MySqlConnector(host, db, user, pass);
                dbConn.OpenConnection();

                var tables = dbConn.GetTableNames();
                if (tables.Count == 0)
                {
                    MessageBox.Show("No se encontraron tablas en la base MySQL.", "Test MySQL");
                    return;
                }

                string firstTable = tables[0];
                var columns = dbConn.GetColumnNames(firstTable);
                var data = dbConn.GetTableData(firstTable, columns);
                string contenido = FormatTableData(data);

                MessageBox.Show(
                    $"MySQL conectado.\nTablas: {string.Join(", ", tables)}\nPrimera tabla: {firstTable}\nColumnas: {string.Join(", ", columns)}\nFilas obtenidas: {data.Rows.Count}\n\nPrimeras filas:\n{contenido}",
                    "Test MySQL");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en test MySQL: {ex.Message}", "Test MySQL");
            }
        }

        public static void TestPostgreSQL(string host, string db, string user, string pass)
        {
            try
            {
                using var dbConn = new PostgreSqlConnector(host, db, user, pass);
                dbConn.OpenConnection();

                var tables = dbConn.GetTableNames();
                if (tables.Count == 0)
                {
                    MessageBox.Show("No se encontraron tablas en la base PostgreSQL.", "Test PostgreSQL");
                    return;
                }

                string firstTable = tables[0];
                var columns = dbConn.GetColumnNames(firstTable);
                var data = dbConn.GetTableData(firstTable, columns);
                string contenido = FormatTableData(data);

                MessageBox.Show(
                    $"PostgreSQL conectado.\nTablas: {string.Join(", ", tables)}\nPrimera tabla: {firstTable}\nColumnas: {string.Join(", ", columns)}\nFilas obtenidas: {data.Rows.Count}\n\nPrimeras filas:\n{contenido}",
                    "Test PostgreSQL");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en test PostgreSQL: {ex.Message}", "Test PostgreSQL");
            }
        }

        public static void TestSqlServer(string host, string db, string user = null, string pass = null, bool useIntegratedSecurity = false)
        {
            try
            {
                string connectionString;

                if (useIntegratedSecurity)
                {
                    connectionString = $"Server={host};Database={db};Integrated Security=True;";
                }
                else
                {
                    connectionString = $"Server={host};Database={db};User Id={user};Password={pass};";
                }

                using var dbConn = new SqlServerConnector(connectionString);
                dbConn.OpenConnection();

                var tables = dbConn.GetTableNames();
                if (tables.Count == 0)
                {
                    MessageBox.Show("No se encontraron tablas en la base SQL Server.", "Test SQL Server");
                    return;
                }

                string firstTable = tables[0];
                var columns = dbConn.GetColumnNames(firstTable);
                var data = dbConn.GetTableData(firstTable, columns);
                string contenido = FormatTableData(data);

                MessageBox.Show(
                    $"SQL Server conectado.\nTablas: {string.Join(", ", tables)}\nPrimera tabla: {firstTable}\nColumnas: {string.Join(", ", columns)}\nFilas obtenidas: {data.Rows.Count}\n\nPrimeras filas:\n{contenido}",
                    "Test SQL Server");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en test SQL Server: {ex.Message}", "Test SQL Server");
            }
        }

        public static void TestSQLite(string databaseFilePath)
        {
            try
            {
                using var dbConn = new SQLiteConnector(databaseFilePath);
                dbConn.OpenConnection();

                var tables = dbConn.GetTableNames();
                if (tables.Count == 0)
                {
                    MessageBox.Show("No se encontraron tablas en la base SQLite.", "Test SQLite");
                    return;
                }

                string firstTable = tables[0];
                var columns = dbConn.GetColumnNames(firstTable);
                var data = dbConn.GetTableData(firstTable, columns);
                string contenido = FormatTableData(data);

                MessageBox.Show(
                    $"SQLite conectado.\nTablas: {string.Join(", ", tables)}\nPrimera tabla: {firstTable}\nColumnas: {string.Join(", ", columns)}\nFilas obtenidas: {data.Rows.Count}\n\nPrimeras filas:\n{contenido}",
                    "Test SQLite");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en test SQLite: {ex.Message}", "Test SQLite");
            }
        }
    }
}
