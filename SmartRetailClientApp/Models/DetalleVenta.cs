using System;
using System.Text.Json.Serialization;

namespace SmartRetailClientApp.Models
{
    /// <summary>
    /// Representa el detalle de una venta, asociando productos con cantidades y subtotales.
    /// </summary>
    public class DetalleVenta
    {
        /// <summary>
        /// Identificador único de la venta a la que pertenece este detalle.
        /// </summary>
        [JsonPropertyName("ventaId")]
        public Guid VentaId { get; set; }

        /// <summary>
        /// Identificador único del producto vendido.
        /// </summary>
        [JsonPropertyName("productoId")]
        public Guid ProductoId { get; set; }

        /// <summary>
        /// Identificador de la tienda donde se realizó la venta.
        /// </summary>
        [JsonPropertyName("tiendaId")]
        public string TiendaId { get; set; }

        /// <summary>
        /// Cantidad del producto vendido. Puede ser nulo si no se especifica.
        /// </summary>
        [JsonPropertyName("cantidad")]
        public int? Cantidad { get; set; }

        /// <summary>
        /// Subtotal correspondiente a este detalle de venta. Puede ser nulo si no se especifica.
        /// </summary>
        [JsonPropertyName("subtotal")]
        public decimal? Subtotal { get; set; }
    }
}
