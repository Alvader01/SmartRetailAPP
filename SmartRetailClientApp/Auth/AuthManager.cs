// Importación de espacios de nombres necesarios
using System.Net.Http;
using System.Text;
using Newtonsoft.Json; 
using SmartRetailClientApp.Config; 

namespace SmartRetailClientApp.Auth
{
    /// <summary>
    /// Clase estática que gestiona la autenticación del usuario en la aplicación.
    /// </summary>
    public static class AuthManager
    {
        /// <summary>
        /// Token de autenticación recibido tras un inicio de sesión exitoso.
        /// Es de solo lectura externa y puede ser usado por otras partes del sistema para autenticarse.
        /// </summary>
        public static string Token { get; private set; } = string.Empty;

        /// <summary>
        /// Intenta iniciar sesión con las credenciales proporcionadas.
        /// </summary>
        /// <param name="username">Nombre de usuario</param>
        /// <param name="password">Contraseña</param>
        /// <returns>True si la autenticación fue exitosa, False en caso contrario</returns>
        public static async Task<bool> LoginAsync(string username, string password)
        {
            // Se crea una instancia de HttpClient dentro de un bloque using para asegurar la liberación de recursos
            using (HttpClient client = new HttpClient())
            {
                // Objeto anónimo que contiene las credenciales del usuario
                var loginData = new
                {
                    username = username,
                    password = password
                };

                // Serialización del objeto de credenciales a formato JSON
                string json = JsonConvert.SerializeObject(loginData);

                // Creación del contenido HTTP con codificación UTF8 y tipo MIME 'application/json'
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                try
                {
                    // Construcción de la URL de login usando la configuración de la aplicación
                    string loginUrl = $"{AppConfig.ApiEndpoint}/api/Auth/login";

                    // Se realiza una petición POST al servidor con las credenciales serializadas
                    HttpResponseMessage response = await client.PostAsync(loginUrl, content);

                    // Verificación del código de estado HTTP
                    if (response.IsSuccessStatusCode)
                    {
                        // Se lee y deserializa la respuesta para extraer el token
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<LoginResponse>(responseContent);

                        // Se guarda el token para su uso futuro
                        Token = result.token;

                        return true; // Inicio de sesión exitoso
                    }

                    // Si la respuesta no fue exitosa, se devuelve false
                    return false;
                }
                catch (Exception ex)
                {
                    // En caso de error (como problemas de red o del servidor), se muestra el mensaje
                    Console.WriteLine("Login error: " + ex.Message);
                    return false;
                }
            }
        }

        /// <summary>
        /// Clase privada usada para deserializar la respuesta del servidor al hacer login.
        /// Debe coincidir con la estructura del JSON recibido.
        /// </summary>
        private class LoginResponse
        {
            public string token { get; set; } // Token JWT u otro tipo recibido desde el backend
        }
    }
}
