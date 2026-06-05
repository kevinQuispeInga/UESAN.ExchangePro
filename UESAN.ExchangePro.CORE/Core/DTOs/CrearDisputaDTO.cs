namespace UESAN.ExchangePro.CORE.Core.DTOs
{
    public class CrearDisputaDTO
    {
        public long IdTransaccion { get; set; }
        public string Motivo { get; set; } = null!;
        public string Descripcion { get; set; } = null!;
    }
}