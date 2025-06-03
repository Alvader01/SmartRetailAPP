using System;
using System.Text.Json.Serialization;

namespace SmartRetailClientApp.Models
{
    /// <summary>
    /// Representa un producto disponible en la tienda, con detalles como nombre, precio y stock.
    /// </summary>
    public class Producto
    {
        /// <summary>
        /// Identificador único del producto.
        /// </summary>
        [JsonPropertyName("productoId")]
        public Guid ProductoId { get; set; }

        /// <summary>
        /// Identificador de la tienda a la que pertenece el producto.
        /// </summary>
        [JsonPropertyName("tiendaId")]
        public string TiendaId { get; set; }

        /// <summary>
        /// Nombre descriptivo del producto.
        /// </summary>
        [JsonPropertyName("nombre")]
        public string? Nombre { get; set; }

        /// <summary>
        /// Precio actual del producto. Puede ser nulo si no está definido.
        /// </summary>
        [JsonPropertyName("precio")]
        public decimal? Precio { get; set; }

        /// <summary>
        /// Cantidad disponible en stock. Puede ser nulo si no se especifica.
        /// </summary>
        [JsonPropertyName("stock")]
        public int? Stock { get; set; }
    }
}
