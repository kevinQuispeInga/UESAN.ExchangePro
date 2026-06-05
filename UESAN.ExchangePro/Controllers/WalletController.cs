using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UESAN.ExchangePro.CORE.Core.DTOs;
using UESAN.ExchangePro.CORE.Core.Entities;
using UESAN.ExchangePro.CORE.Core.Interfaces;

namespace UESAN.ExchangePro.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly IWalletRepository _walletRepository;

        public WalletController(IWalletRepository walletRepository)
        {
            _walletRepository = walletRepository;
        }

        [HttpGet("saldo")]
        public async Task<IActionResult> GetSaldo()
        {
            var idUsuario = long.Parse(User.FindFirst("IdUsuario")?.Value ?? "0");
            var wallet = await _walletRepository.GetByUsuarioId(idUsuario);

            if (wallet == null) return NotFound("Wallet no encontrada.");

            // Devolvemos la lista de saldos por moneda
            var saldos = wallet.WalletSaldos.Select(s => new {
                idMoneda = s.IdMoneda,
                saldoDisponible = s.SaldoDisponible,
                saldoRetenido = s.SaldoRetenido
            });

            return Ok(new { idWallet = wallet.IdWallet, saldos = saldos });
        }

        [HttpPost("recargar")]
        public async Task<IActionResult> Recargar([FromBody] RecargaDTO dto)
        {
            if (dto.Monto <= 0) return BadRequest("El monto debe ser mayor a cero.");

            var idUsuario = long.Parse(User.FindFirst("IdUsuario")?.Value ?? "0");
            var wallet = await _walletRepository.GetByUsuarioId(idUsuario);

            if (wallet == null) return NotFound("Wallet no encontrada.");

            // Buscamos si el usuario ya tiene un registro de saldo para esta moneda
            var saldoMoneda = wallet.WalletSaldos.FirstOrDefault(s => s.IdMoneda == dto.IdMoneda);

            if (saldoMoneda != null)
            {
                // Si existe, le sumamos al SaldoDisponible
                saldoMoneda.SaldoDisponible = (saldoMoneda.SaldoDisponible ?? 0) + dto.Monto;
            }
            else
            {
                // Si es la primera vez que recarga esta moneda, creamos el registro
                wallet.WalletSaldos.Add(new WalletSaldos
                {
                    IdMoneda = dto.IdMoneda,
                    SaldoDisponible = dto.Monto,
                    SaldoRetenido = 0
                });
            }

            bool actualizado = await _walletRepository.Update(wallet);

            if (actualizado)
                return Ok(new { mensaje = "Saldo recargado correctamente." });

            return BadRequest("Error al procesar la recarga.");
        }
    }
}