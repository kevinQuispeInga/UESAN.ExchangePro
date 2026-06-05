namespace UESAN.ExchangePro.CORE.Core.DTOs
{
    public class PublicarOfertaDTO
    {
        public int MonedaEntrega { get; set; }
        public int MonedaRecibe { get; set; }
        public decimal MontoOfertado { get; set; }
        public decimal MontoMinimo { get; set; }
        public decimal TasaCambio { get; set; }
        public string TipoOperacion { get; set; } = null!;
    }
}