using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using UESAN.ExchangePro.CORE.Core.DTOs;
using UESAN.ExchangePro.CORE.Core.Entities;
using UESAN.ExchangePro.CORE.Core.Interfaces;

namespace UESAN.ExchangePro.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Todo el flujo requiere que el usuario esté logueado mediante Token JWT
    public class DisputaController : ControllerBase
    {
        private readonly IDisputaRepository _disputaRepo;

        public DisputaController(IDisputaRepository disputaRepo)
        {
            _disputaRepo = disputaRepo;
        }

        // =========================================================================
        // 1. POST: api/Disputa/abrir
        // =========================================================================
        [HttpPost("abrir")]
        public async Task<IActionResult> AbrirDisputa([FromBody] CrearDisputaDTO dto)
        {
            var idUsuarioClaim = User.FindFirst("IdUsuario")?.Value;
            if (string.IsNullOrEmpty(idUsuarioClaim))
                return Unauthorized("Token inválido o expirado.");

            long idUsuario = long.Parse(idUsuarioClaim);

            // Armamos el objeto Disputa basado en lo que mandó el usuario
            var nuevaDisputa = new Disputas
            {
                IdTransaccion = dto.IdTransaccion,
                UsuarioReporta = idUsuario,
                Motivo = dto.Motivo,
                Descripcion = dto.Descripcion
                // Nota: El Estado ('PENDIENTE') y la Fecha se asignan automáticamente en el Repositorio
            };

            try
            {
                bool exito = await _disputaRepo.AbrirDisputa(nuevaDisputa);

                if (exito)
                    return Ok(new { mensaje = "Disputa abierta. La transacción ha sido CONGELADA en estado 'EN_DISPUTA' hasta que un administrador la revise." });

                return BadRequest("No se pudo abrir la disputa. Verifica que la transacción sea válida.");
            }
            catch (Exception ex)
            {
                return BadRequest($"ERROR AL ABRIR DISPUTA: {ex.Message}");
            }
        }

        // =========================================================================
        // 2. GET: api/Disputa/pendientes
        // =========================================================================
        [HttpGet("pendientes")]
        public async Task<IActionResult> ListarDisputasPendientes()
        {
            // BARRERA DE SEGURIDAD: Solo el Rol 2 (Admin) puede ver la bandeja del VAR
            var rolClaim = User.FindFirst("Rol")?.Value;
            if (rolClaim != "2")
            {
                return StatusCode(403, new { mensaje = "Acceso denegado. Solo un Administrador puede ver la bandeja del VAR." });
            }

            try
            {
                var disputas = await _disputaRepo.GetDisputasPendientes();
                return Ok(disputas);
            }
            catch (Exception ex)
            {
                return BadRequest($"ERROR AL LISTAR DISPUTAS PENDIENTES: {ex.Message}");
            }
        }

        // =========================================================================
        // 3. PUT: api/Disputa/resolver
        // =========================================================================
        [HttpPut("resolver")]
        public async Task<IActionResult> ResolverDisputa([FromBody] ResolverDisputaDTO dto)
        {
            var idUsuarioClaim = User.FindFirst("IdUsuario")?.Value;
            var rolClaim = User.FindFirst("Rol")?.Value;

            if (string.IsNullOrEmpty(idUsuarioClaim))
                return Unauthorized("Token inválido o sesión expirada.");

            // BARRERA DE SEGURIDAD: Solo el Rol 2 (Admin) puede dictar sentencia
            if (rolClaim != "2")
            {
                return StatusCode(403, new { mensaje = "Acceso denegado. Solo un Administrador puede ingresar a la sala del VAR." });
            }

            long idAdmin = long.Parse(idUsuarioClaim);

            try
            {
                // Ejecutamos la lógica de resolución transaccional en la Base de Datos
                bool exito = await _disputaRepo.ResolverDisputa(dto.IdDisputa, idAdmin, dto.Decision, dto.Observacion);

                if (exito)
                {
                    string msjExtra = dto.Decision.ToUpper() == "A_FAVOR_COMPRADOR"
                        ? "Fondos liberados a la Wallet del comprador."
                        : "Transacción anulada y stock de la oferta devuelto al mercado P2P.";

                    return Ok(new { mensaje = $"Fallo emitido exitosamente por el VAR. {msjExtra}" });
                }

                return BadRequest("No se pudo procesar la resolución. Verifica que el ID de la disputa sea correcto.");
            }
            catch (Exception ex)
            {
                return BadRequest($"ERROR CRÍTICO EN LA SALA DEL VAR: {ex.Message}");
            }
        }
    }
}