using System.Text.Json.Serialization;

namespace SmartRetailClientApp.Models
{
    /// <summary>
    /// Representa un cliente en el sistema con información básica de contacto y asociación a tienda.
    /// </summary>
    public class Cliente
    {
        /// <summary>
        /// Identificador único del cliente (GUID).
        /// </summary>
        [JsonPropertyName("clienteId")]
        public Guid ClienteId { get; set; }

        /// <summary>
        /// Identificador de la tienda asociada al cliente.
        /// </summary>
        [JsonPropertyName("tiendaId")]
        public string TiendaId { get; set; }

        /// <summary>
        /// Nombre completo del cliente. Puede ser nulo si no se proporciona.
        /// </summary>
        [JsonPropertyName("nombre")]
        public string? Nombre { get; set; }

        /// <summary>
        /// Correo electrónico del cliente. Puede ser nulo si no se proporciona.
        /// </summary>
        [JsonPropertyName("correo")]
        public string? Correo { get; set; }

        /// <summary>
        /// Número telefónico del cliente. Puede ser nulo si no se proporciona.
        /// </summary>
        [JsonPropertyName("telefono")]
        public string? Telefono { get; set; }
    }
}
