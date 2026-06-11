using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.Text;
using TiendaVirtual.Api.Extensiones;
using TiendaVirtual.Dominio;
using TiendaVirtual.Dominio.Utilidad;
using TiendaVirtual.Intercambio;

namespace TiendaVirtual.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // PostgreSQL: conexión a la base de datos
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            builder.Services.AddDbContext<TiendaVirtualDbContext>(options =>
                options.UseNpgsql(connectionString));

            builder.Services.AddHttpContextAccessor();

            // JWT Authentication
            var jwtSettings = builder.Configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; // false en desarrollo, true en producción
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
            });

            // CORS: permite que el frontend React+Vite consuma el API
            var allowedOrigins = builder.Configuration
                .GetSection("Cors:UrlPermitidas").Get<string[]>();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("MisOrigenesPermitidos", corsBuilder =>
                {
                    corsBuilder.WithOrigins(allowedOrigins ?? Array.Empty<string>())
                               .AllowAnyMethod()
                               .AllowAnyHeader()
                               .AllowCredentials();
                });
            });

            // Registro de servicios de los módulos
            builder.Services.AgregarServiciosSeguridad();
            builder.Services.AgregarServiciosVendedor();
            builder.Services.AgregarServiciosCatalogo();
            builder.Services.AgregarServiciosVenta();
            builder.Services.AgregarServiciosSuscripcion();
            builder.Services.AgregarServiciosSoporte();

            // Controllers + JSON (enums como string, camelCase)
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                    options.JsonSerializerOptions.PropertyNamingPolicy =
                        System.Text.Json.JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.Converters.Add(
                        new System.Text.Json.Serialization.JsonStringEnumConverter());
                });

            // Interceptor de ModelState (errores 400 traducidos al español)
            builder.Services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errores = context.ModelState
                        .Where(e => e.Value?.Errors.Count > 0)
                        .Select(e =>
                        {
                            var campo = e.Key;
                            var mensajeOriginal = e.Value!.Errors.First().ErrorMessage;
                            var mensajeTraducido =
                                TraductorErroresValidacion.Traducir(mensajeOriginal, campo);
                            return new ValidationResult(mensajeTraducido, new[] { campo });
                        })
                        .ToList();

                    var mensaje = string.Join("\r\n", errores.Select(e =>
                        $"Error en {string.Join(", ", e.MemberNames)}: {e.ErrorMessage}"));

                    var resultado = new ResultadoOperacion<List<ValidationResult>>
                    {
                        Exito = false,
                        Mensaje = mensaje,
                        Datos = errores
                    };

                    return new BadRequestObjectResult(resultado);
                };
            });

            builder.Services.AddOpenApi();

            // Build & Pipeline
            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseCors("MisOrigenesPermitidos");

            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}