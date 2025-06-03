using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using SmartRetailClientApp.Logging;

namespace SmartRetailClientApp.Uploader
{
    public static class ApiUploader
    {
        // HttpClient estático para reutilización (aunque aquí usas uno local en el método)
        private static readonly HttpClient client = new HttpClient();

        /// <summary>
        /// Envía una lista de diccionarios serializados en JSON como POST a la API.
        /// </summary>
        /// <param name="data">Lista de registros a enviar</param>
        /// <param name="url">URL completa del endpoint</param>
        /// <param name="token">Token Bearer para autorización</param>
        /// <returns>True si el POST fue exitoso, false en caso contrario</returns>
        public static async Task<bool> UploadDataAsync(List<Dictionary<string, object>> data, string url, string token)
        {
            try
            {
                // Se crea un HttpClient local en el método (podrías usar el static arriba para eficiencia)
                using var client = new HttpClient();

                // Asignar token Bearer en cabecera Authorization
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Serializar la lista de datos a JSON
                var json = JsonConvert.SerializeObject(data);

                // Crear contenido HTTP con tipo application/json y codificación UTF-8
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Logger.WriteLog($"Enviando POST a {url} con {data.Count} registros...");

                // Realizar el POST async
                var response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    Logger.WriteLog($"POST a {url} exitoso.");
                    return true;
                }
                else
                {
                    // Leer cuerpo de respuesta para debug en caso de error
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Logger.WriteLog($"Error en POST a {url}. Código: {response.StatusCode}. Respuesta: {responseBody}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"Excepción en UploadDataAsync: {ex.Message}");
                return false;
            }
        }
    }
}
