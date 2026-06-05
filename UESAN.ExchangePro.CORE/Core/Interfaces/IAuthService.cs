using System.Threading.Tasks;
using UESAN.ExchangePro.CORE.Core.DTOs;

namespace UESAN.ExchangePro.CORE.Core.Interfaces
{
    public interface IAuthService
    {
        Task<bool> Registrar(RegistroDTO registroDTO);
        Task<string> Login(LoginDTO loginDTO);
    }
}