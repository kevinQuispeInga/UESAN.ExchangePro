using System.Threading.Tasks;
using UESAN.ExchangePro.CORE.Core.Entities;

namespace UESAN.ExchangePro.CORE.Core.Interfaces
{
    public interface IDisputaRepository
    {
        // El método para que un usuario abra un reclamo
        Task<bool> AbrirDisputa(Disputas disputa);
        Task<bool> ResolverDisputa(long idDisputa, long idAdmin, string decision, string observacion);
    }
}