using System.Net.Http;
using System.Net.Http.Headers;
using SmartRetailClientApp.Logging;

namespace SmartRetailClientApp.Getter
{
    /// <summary>
    /// Clase estática para obtener datos desde una API REST mediante llamadas HTTP GET.
    /// </summary>
    public static class ApiGetter
    {
        // HttpClient estático para reutilizar la misma instancia y evitar problemas de socket exhaustion.
        private static readonly HttpClient client = new HttpClient();

        /// <summary>
        /// Realiza una llamada HTTP GET asíncrona a la URL especificada usando un token Bearer para autorización.
        /// Devuelve el contenido JSON como string si la llamada es exitosa, o null en caso de error.
        /// </summary>
        /// <param name="url">URL completa del endpoint API.</param>
        /// <param name="bearerToken">Token de autorización tipo Bearer.</param>
        /// <returns>Cadena JSON con la respuesta o null si ocurre algún error.</returns>
        public static async Task<string> GetDataAsync(string url, string bearerToken)
        {
            try
            {
                // Asignar token Bearer al header Authorization
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                // Realizar la solicitud GET
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    // Retornar el contenido de la respuesta en caso de éxito
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    // Leer el contenido del error y loguear
                    string error = await response.Content.ReadAsStringAsync();
                    Logger.WriteLog($"Error GET {url}: {response.StatusCode} - {error}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                // Capturar cualquier excepción y registrar el mensaje
                Logger.WriteLog($"Exception en GetDataAsync: {ex.Message}");
                return null;
            }
        }
    }
}
