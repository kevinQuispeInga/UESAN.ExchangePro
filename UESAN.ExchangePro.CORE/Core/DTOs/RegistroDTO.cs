namespace UESAN.ExchangePro.CORE.Core.DTOs
{
    public class RegistroDTO
    {
        public string NombreCompleto { get; set; } = null!;
        public string Correo { get; set; } = null!;
        public string Telefono { get; set; } = null!;
        public string DocumentoIdentidad { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string ConfirmarPassword { get; set; } = null!;
    }
}