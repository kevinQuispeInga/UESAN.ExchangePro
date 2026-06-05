using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using UESAN.ExchangePro.CORE.Core.DTOs;
using UESAN.ExchangePro.CORE.Core.Entities;
using UESAN.ExchangePro.CORE.Core.Interfaces;

namespace UESAN.ExchangePro.CORE.Core.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IWalletRepository _walletRepository;
        private readonly IConfiguration _config;

        public AuthService(IUsuarioRepository usuarioRepository, IWalletRepository walletRepository, IConfiguration config)
        {
            _usuarioRepository = usuarioRepository;
            _walletRepository = walletRepository;
            _config = config;
        }

        public async Task<bool> Registrar(RegistroDTO registroDTO)
        {
            // 1. Validación básica de contraseñas
            if (registroDTO.Password != registroDTO.ConfirmarPassword)
                throw new Exception("Las contraseñas no coinciden.");

            // 2. Verificación de existencia (seguridad)
            bool existe = await _usuarioRepository.CorreoExiste(registroDTO.Correo);
            if (existe)
                throw new Exception("El correo ya está registrado.");

            // 3. Creación del objeto Usuario
            // NOTA: Al no asignar 'IdRolNavigation', evitamos que EF Core intente 
            // insertar o validar la tabla Roles innecesariamente.
            var nuevoUsuario = new Usuarios
            {
                IdRol = 1, // Rol USER por defecto
                NombreCompleto = registroDTO.NombreCompleto,
                Correo = registroDTO.Correo,
                Telefono = registroDTO.Telefono,
                DocumentoIdentidad = registroDTO.DocumentoIdentidad,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registroDTO.Password),
                Estado = "ACTIVO",
                Reputacion = 5.00m,
                TotalCalificaciones = 0,
                FechaRegistro = DateTime.UtcNow
            };

            // 4. Inserción
            bool usuarioInsertado = await _usuarioRepository.Insert(nuevoUsuario);

            // 5. Creación de la Wallet asociada
            if (usuarioInsertado)
            {
                var nuevaWallet = new Wallets
                {
                    IdUsuario = nuevoUsuario.IdUsuario, // EF Core debería tener el ID tras el Insert
                    FechaCreacion = DateTime.UtcNow
                };
                await _walletRepository.Insert(nuevaWallet);
                return true;
            }

            return false;
        }

        public async Task<string> Login(LoginDTO loginDTO)
        {
            var usuario = await _usuarioRepository.GetByCorreo(loginDTO.Correo);
            if (usuario == null)
                throw new Exception("Credenciales incorrectas.");

            bool passwordValido = BCrypt.Net.BCrypt.Verify(loginDTO.Password, usuario.PasswordHash);
            if (!passwordValido)
                throw new Exception("Credenciales incorrectas.");

            // GENERACIÓN DEL TOKEN JWT
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? throw new InvalidOperationException("Falta la clave JWT en appsettings")));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Correo),
                new Claim("IdUsuario", usuario.IdUsuario.ToString()),
               new Claim("Rol", usuario.IdRol.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}