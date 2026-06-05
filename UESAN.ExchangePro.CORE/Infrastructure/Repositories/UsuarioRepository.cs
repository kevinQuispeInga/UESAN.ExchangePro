using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using UESAN.ExchangePro.CORE.Core.Entities;
using UESAN.ExchangePro.CORE.Core.Interfaces;
using UESAN.ExchangePro.CORE.Infrastructure.Data;

namespace UESAN.ExchangePro.CORE.Infrastructure.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly ExchangeProDbContext _context;

        public UsuarioRepository(ExchangeProDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CorreoExiste(string correo)
        {
            return await _context.Usuarios.AnyAsync(u => u.Correo == correo);
        }

        public async Task<Usuarios> GetByCorreo(string correo)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo);
        }

        public async Task<bool> Insert(Usuarios usuario)
        {
            await _context.Usuarios.AddAsync(usuario);
            int rows = await _context.SaveChangesAsync();
            return rows > 0;
        }
    }
}