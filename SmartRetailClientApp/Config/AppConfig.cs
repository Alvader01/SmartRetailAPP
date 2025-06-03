namespace SmartRetailClientApp.Config
{
    /// <summary>
    /// Clase estática de configuración de la aplicación.
    /// Contiene parámetros globales accesibles desde cualquier parte del proyecto.
    /// </summary>
    public static class AppConfig
    {
        /// <summary>
        /// URL base del servicio API que utiliza la aplicación cliente.
        /// Este endpoint es utilizado para realizar todas las peticiones HTTP hacia el backend.
        /// </summary>
        public static string ApiEndpoint => "https://smart-retail-api.onrender.com";
    }
}
