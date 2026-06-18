using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // IMPORTANTE: Necesario para IFormFile
using System;
using System.IO; // IMPORTANTE: Necesario para manejar carpetas y archivos
using System.Linq;
using System.Threading.Tasks;
using UESAN.ExchangePro.CORE.Core.DTOs;
using UESAN.ExchangePro.CORE.Core.Entities;
using UESAN.ExchangePro.CORE.Core.Interfaces;

namespace UESAN.ExchangePro.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Todo el flujo requiere que el usuario esté logueado
    public class TransaccionController : ControllerBase
    {
        private readonly ITransaccionRepository _transRepo;
        private readonly IOfertaRepository _ofertaRepo;

        public TransaccionController(ITransaccionRepository transRepo, IOfertaRepository ofertaRepo)
        {
            _transRepo = transRepo;
            _ofertaRepo = ofertaRepo;
        }

        // POST: api/Transaccion
        [HttpPost]
        public async Task<IActionResult> IniciarTransaccion([FromBody] CrearTransaccionDTO dto)
        {
            var idCompradorClaim = User.FindFirst("IdUsuario")?.Value;
            if (string.IsNullOrEmpty(idCompradorClaim))
                return Unauthorized("Usuario no identificado.");

            long idComprador = long.Parse(idCompradorClaim);

            // 1. Obtener la oferta
            var oferta = await _ofertaRepo.GetById(dto.IdOferta);

            // 2. Validaciones básicas
            if (oferta == null) return NotFound("Oferta no encontrada.");
            if (oferta.Estado != "ACTIVA") return BadRequest("La oferta no está disponible.");
            if (oferta.IdUsuario == idComprador) return BadRequest("No puedes tomar tu propia oferta.");

            // 2b. Validar que Monto esté entre MontoMinimo y MontoOfertado
            if (dto.Monto < oferta.MontoMinimo)
                return BadRequest($"El monto mínimo para esta oferta es {oferta.MontoMinimo}.");
            if (dto.Monto > oferta.MontoOfertado)
                return BadRequest($"El monto no puede exceder el monto ofertado ({oferta.MontoOfertado}).");

            // 3. Crear el objeto transacción usando el monto proporcionado
            var transaccion = new Transacciones
            {
                Codigo = "TRX-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                IdOferta = oferta.IdOferta,
                CompradorId = idComprador,
                VendedorId = oferta.IdUsuario,
                MontoOperacion = dto.Monto,
                Estado = "PENDIENTE",
                FechaInicio = DateTime.UtcNow,
                IdMetodoPago = dto.IdMetodoPago
            };

            // 4. Guardar en base de datos capturando el error real
            try
            {
                bool exito = await _transRepo.CrearTransaccion(transaccion, oferta);
                return Ok(new { mensaje = "Transacción iniciada correctamente.", idTransaccion = transaccion.IdTransaccion });
            }
            catch (Exception ex)
            {
                return BadRequest($"ERROR DE BASE DE DATOS: {ex.Message}");
            }
        }

        // GET: api/Transaccion/mis-operaciones
        [HttpGet("mis-operaciones")]
        public async Task<IActionResult> GetMisOperaciones()
        {
            var idUsuario = long.Parse(User.FindFirst("IdUsuario")?.Value ?? "0");

            // Obtenemos las transacciones desde la base de datos
            var transacciones = await _transRepo.GetByUsuario(idUsuario);

            // Mapeamos a DTO para evitar el error 500 de referencias circulares
            var listaDTO = transacciones.Select(t => new TransaccionResponseDTO
            {
                IdTransaccion = t.IdTransaccion,
                IdOferta = t.IdOferta,
                CompradorId = t.CompradorId,
                VendedorId = t.VendedorId,
                MontoOperacion = t.MontoOperacion,
                Estado = t.Estado,
                FechaInicio = t.FechaInicio,
                Codigo = t.Codigo
            });

            return Ok(listaDTO);
        }

        // PUT: api/Transaccion/estado
        [HttpPut("estado")]
        public async Task<IActionResult> ActualizarEstado([FromBody] ActualizarEstadoDTO dto)
        {
            // 1. Validar que el estado sea uno de los permitidos
            var estadosValidos = new[] { "PAGADO", "COMPLETADO", "CANCELADO" };
            if (!estadosValidos.Contains(dto.NuevoEstado.ToUpper()))
                return BadRequest("Estado no válido.");

            // 2. Aquí llamaremos al repositorio para hacer el cambio en la BD
            bool exito = await _transRepo.ActualizarEstado(dto.IdTransaccion, dto.NuevoEstado.ToUpper());

            if (exito)
                return Ok(new { mensaje = $"La transacción ha cambiado a estado: {dto.NuevoEstado.ToUpper()}" });

            return BadRequest("No se pudo actualizar el estado. Verifica que la transacción exista.");
        }

        // POST: api/Transaccion/liberar
        [HttpPost("liberar")]
        public async Task<IActionResult> LiberarFondos([FromBody] LiberarFondosDTO dto)
        {
            var idVendedorClaim = User.FindFirst("IdUsuario")?.Value;
            if (string.IsNullOrEmpty(idVendedorClaim))
                return Unauthorized("Usuario no identificado.");

            long idVendedor = long.Parse(idVendedorClaim);

            try
            {
                bool exito = await _transRepo.LiberarFondos(dto.IdTransaccion, idVendedor);

                if (exito)
                    return Ok(new { mensaje = "¡Éxito! Fondos liberados y transacción completada." });

                return BadRequest("No se pudo completar la operación.");
            }
            catch (Exception ex)
            {
                return BadRequest($"ERROR AL LIBERAR FONDOS: {ex.Message}");
            }
        }

        // PUT: api/Transaccion/{id}/cancelar
        [HttpPut("{id}/cancelar")]
        public async Task<IActionResult> CancelarTransaccion(long id)
        {
            var idUsuarioClaim = User.FindFirst("IdUsuario")?.Value;
            if (string.IsNullOrEmpty(idUsuarioClaim))
                return Unauthorized("Usuario no identificado.");

            long idUsuario = long.Parse(idUsuarioClaim);

            try
            {
                bool exito = await _transRepo.CancelarTransaccion(id, idUsuario);

                if (exito)
                    return Ok(new { mensaje = "Transacción cancelada. La oferta ha vuelto a estar ACTIVA en el mercado." });

                return BadRequest("No se pudo cancelar la transacción.");
            }
            catch (Exception ex)
            {
                return BadRequest($"ERROR AL CANCELAR: {ex.Message}");
            }
        }

        // GET: api/Transaccion/{id}/instrucciones
        [HttpGet("{id}/instrucciones")]
        public async Task<IActionResult> GetInstruccionesPago(long id)
        {
            var idUsuarioClaim = User.FindFirst("IdUsuario")?.Value;
            if (string.IsNullOrEmpty(idUsuarioClaim)) return Unauthorized();
            long idUsuario = long.Parse(idUsuarioClaim);

            // 1. Traemos la transacción
            var transaccion = await _transRepo.GetTransaccionConMetodoPago(id);
            if (transaccion == null) return NotFound("Transacción no encontrada.");

            if (transaccion.CompradorId != idUsuario && transaccion.VendedorId != idUsuario)
                return BadRequest("No tienes acceso a esta transacción.");

            // 2. Traemos los datos de pago reales del vendedor
            var datosVendedor = await _transRepo.GetDatosPagoVendedor(transaccion.VendedorId);
            if (datosVendedor == null)
                return BadRequest("El vendedor no tiene datos de pago registrados en el sistema.");

            var metodoPagoNombre = transaccion.IdMetodoPagoNavigation?.Nombre ?? "Desconocido";

            // 3. Armamos la respuesta con los datos de tu tabla DatosPagoUsuario
            var instrucciones = new
            {
                MetodoSeleccionado = metodoPagoNombre,
                MontoADepositar = transaccion.MontoOperacion,
                DatosDelVendedor = new
                {
                    Yape = datosVendedor.Yape,
                    Plin = datosVendedor.Plin,
                    NumeroCuenta = datosVendedor.NumeroCuenta,
                    CCI = datosVendedor.Cci
                },
                Mensaje = $"Por favor, transfiere exactamente {transaccion.MontoOperacion} usando {metodoPagoNombre}. Una vez transferido, marca la transacción como PAGADA."
            };

            return Ok(instrucciones);
        }

        // =========================================================================
        // NUEVO: PUT para marcar como pagado y subir el voucher (El camino profesional)
        // =========================================================================

        // PUT: api/Transaccion/{id}/pagar
        [HttpPut("{id}/pagar")]
        public async Task<IActionResult> MarcarComoPagado(long id, [FromForm] IFormFile comprobante)
        {
            var idUsuarioClaim = User.FindFirst("IdUsuario")?.Value;
            if (string.IsNullOrEmpty(idUsuarioClaim)) return Unauthorized("Token inválido.");
            long idUsuario = long.Parse(idUsuarioClaim);

            // 1. Validar que realmente enviaron un archivo
            if (comprobante == null || comprobante.Length == 0)
                return BadRequest("Debes adjuntar el comprobante de pago (imagen o PDF).");

            try
            {
                // 2. Crear la carpeta "wwwroot/vouchers" si no existe
                string carpetaDestino = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "vouchers");
                if (!Directory.Exists(carpetaDestino))
                    Directory.CreateDirectory(carpetaDestino);

                // 3. Generar un nombre único para el archivo (Ej: VOUCHER_8_abc123.jpg)
                string extension = Path.GetExtension(comprobante.FileName);
                string nombreArchivo = $"VOUCHER_{id}_{Guid.NewGuid().ToString().Substring(0, 6)}{extension}";
                string rutaFisica = Path.Combine(carpetaDestino, nombreArchivo);

                // 4. Copiar el archivo físicamente al servidor
                using (var stream = new FileStream(rutaFisica, FileMode.Create))
                {
                    await comprobante.CopyToAsync(stream);
                }

                // 5. Guardar la ruta relativa en la base de datos
                string rutaParaBD = $"/vouchers/{nombreArchivo}";
                bool exito = await _transRepo.MarcarComoPagado(id, idUsuario, rutaParaBD);

                if (exito)
                    return Ok(new { mensaje = "Comprobante subido y transacción marcada como PAGADA exitosamente.", ruta = rutaParaBD });

                return BadRequest("No se pudo actualizar el estado de la transacción.");
            }
            catch (Exception ex)
            {
                return BadRequest($"ERROR AL PROCESAR EL PAGO: {ex.Message}");
            }
        }
    }
}