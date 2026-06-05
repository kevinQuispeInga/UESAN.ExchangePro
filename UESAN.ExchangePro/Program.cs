using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using UESAN.ExchangePro.CORE.Core.Interfaces;
using UESAN.ExchangePro.CORE.Core.Services;
using UESAN.ExchangePro.CORE.Infrastructure.Data;
using UESAN.ExchangePro.CORE.Infrastructure.Repositories;
using UESAN.ExchangePro.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuración de Base de Datos
builder.Services.AddDbContext<ExchangeProDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Registro de Repositorios y Servicios (Inyección de Dependencias)
builder.Services.AddTransient<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddTransient<IWalletRepository, WalletRepository>();
builder.Services.AddTransient<IAuthService, AuthService>();
builder.Services.AddTransient<IDatosPagoRepository, DatosPagoRepository>();
builder.Services.AddTransient<IOfertaRepository, OfertaRepository>();
builder.Services.AddTransient<ITransaccionRepository, TransaccionRepository>();
builder.Services.AddTransient<IDisputaRepository, DisputaRepository>();
// 3. Configuración de Seguridad JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is missing in appsettings.json")))
        };
    });

builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// MUY IMPORTANTE: UseAuthentication siempre debe ir ANTES de UseAuthorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();