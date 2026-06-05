namespace UESAN.ExchangePro.CORE.Core.DTOs
{
    public class ResolverDisputaDTO
    {
        public long IdDisputa { get; set; }
        public string Decision { get; set; } = null!; // Opciones: "A_FAVOR_COMPRADOR" o "A_FAVOR_VENDEDOR"
        public string Observacion { get; set; } = null!;
    }
}