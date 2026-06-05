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
    [Authorize] // Siempre protegemos con Token
    public class DisputaController : ControllerBase
    {
        private readonly IDisputaRepository _disputaRepo;

        public DisputaController(IDisputaRepository disputaRepo)
        {
            _disputaRepo = disputaRepo;
        }

        // POST: api/Disputa/abrir
        [HttpPost("abrir")]
        public async Task<IActionResult> AbrirDisputa([FromBody] CrearDisputaDTO dto)
        {
            var idUsuarioClaim = User.FindFirst("IdUsuario")?.Value;
            if (string.IsNullOrEmpty(idUsuarioClaim))
                return Unauthorized("Token inválido.");

            long idUsuario = long.Parse(idUsuarioClaim);

            // 1. Armamos el objeto Disputa basado en lo que mandó el usuario
            var nuevaDisputa = new Disputas
            {
                IdTransaccion = dto.IdTransaccion,
                UsuarioReporta = idUsuario,
                Motivo = dto.Motivo,
                Descripcion = dto.Descripcion
                // El Estado y la Fecha se llenan automáticamente en el Repositorio
            };

            // 2. Ejecutamos la jugada
            try
            {
                bool exito = await _disputaRepo.AbrirDisputa(nuevaDisputa);

                if (exito)
                    return Ok(new { mensaje = "Disputa abierta. La transacción ha sido CONGELADA en estado 'EN_DISPUTA' hasta que un administrador la revise." });

                return BadRequest("No se pudo abrir la disputa.");
            }
            catch (Exception ex)
            {
                return BadRequest($"ERROR AL ABRIR DISPUTA: {ex.Message}");
            }
        }
        // PUT: api/Disputa/resolver
        [HttpPut("resolver")]
        public async Task<IActionResult> ResolverDisputa([FromBody] ResolverDisputaDTO dto)
        {
            // 1. Extraemos las claims del token
            var idUsuarioClaim = User.FindFirst("IdUsuario")?.Value;
            var rolClaim = User.FindFirst("Rol")?.Value; // Asumiendo que guardaste 'Rol' en el token

            // 2. Verificación de existencia de token
            if (string.IsNullOrEmpty(idUsuarioClaim))
                return Unauthorized("Token inválido.");

            // 3. BARRERA DE SEGURIDAD: Solo el Rol 2 (Admin) puede pasar
            if (rolClaim != "2")
            {
                return StatusCode(403, new { mensaje = "Acceso denegado. Solo un Administrador puede ingresar a la sala del VAR." });
            }

            long idAdmin = long.Parse(idUsuarioClaim);

            try
            {
                // 4. Ejecutamos la lógica de resolución
                bool exito = await _disputaRepo.ResolverDisputa(dto.IdDisputa, idAdmin, dto.Decision, dto.Observacion);

                if (exito)
                {
                    string msjExtra = dto.Decision.ToUpper() == "A_FAVOR_COMPRADOR"
                        ? "Fondos liberados al comprador."
                        : "Transacción anulada y oferta devuelta al mercado.";

                    return Ok(new { mensaje = $"Fallo emitido exitosamente. {msjExtra}" });
                }

                return BadRequest("No se pudo resolver la disputa.");
            }
            catch (Exception ex)
            {
                // Registro del error y respuesta al usuario
                return BadRequest($"ERROR EN EL VAR: {ex.Message}");
            }
        }
    }
}