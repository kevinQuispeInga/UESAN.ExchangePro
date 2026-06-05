using Microsoft.AspNetCore.Mvc;
using UESAN.ExchangePro.CORE.Core.DTOs;
using UESAN.ExchangePro.CORE.Core.Interfaces;

namespace UESAN.ExchangePro.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar([FromBody] RegistroDTO registroDTO)
        {
            try
            {
                bool resultado = await _authService.Registrar(registroDTO);
                if (resultado)
                    return Ok(new { mensaje = "Usuario registrado y Wallet creada con éxito." });

                return BadRequest("No se pudo registrar el usuario.");
            }
            catch (Exception ex)
            {
                // Esto captura el error real de la base de datos
                var mensaje = ex.Message;
                if (ex.InnerException != null)
                    mensaje += " | Detalle: " + ex.InnerException.Message;

                return BadRequest(new { error = mensaje });
            }
           
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
        {
            try
            {
                string token = await _authService.Login(loginDTO);
                return Ok(new { token = token });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }
    }
}