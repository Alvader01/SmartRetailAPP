using System.Text.Json.Serialization;

namespace SmartRetailClientApp.Models
{
    /// <summary>
    /// Representa una venta realizada en la tienda, incluyendo información del cliente y detalles de la transacción.
    /// </summary>
    public class Venta
    {
        /// <summary>
        /// Identificador único de la venta.
        /// </summary>
        [JsonPropertyName("ventaId")]
        public Guid VentaId { get; set; }

        /// <summary>
        /// Identificador de la tienda donde se realizó la venta.
        /// </summary>
        [JsonPropertyName("tiendaId")]
        public string TiendaId { get; set; }

        /// <summary>
        /// Fecha y hora en la que se realizó la venta. Puede ser nula si no está especificada.
        /// </summary>
        [JsonPropertyName("fecha")]
        public DateTime? Fecha { get; set; }

        /// <summary>
        /// Total monetario de la venta. Puede ser nulo si no está definido.
        /// </summary>
        [JsonPropertyName("total")]
        public decimal? Total { get; set; }

        /// <summary>
        /// Identificador opcional del cliente asociado a la venta.
        /// </summary>
        [JsonPropertyName("clienteId")]
        public Guid? ClienteId { get; set; }

        /// <summary>
        /// Información del cliente asociado a la venta. Puede ser nula si no está cargada.
        /// </summary>
        [JsonPropertyName("cliente")]
        public Cliente? Cliente { get; set; }
    }
}
