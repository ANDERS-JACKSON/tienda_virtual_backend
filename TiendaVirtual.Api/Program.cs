using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

            // PostgreSQL: conexi�n a la base de datos
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            builder.Services.AddDbContext<TiendaVirtualDbContext>(options =>
                options.UseNpgsql(connectionString));

            builder.Services.AddMemoryCache();
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
                options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
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

            // Registro de servicios de los m�dulos
            builder.Services.AgregarServiciosSoporte();
            builder.Services.AgregarServiciosConfiguracion();
            builder.Services.AgregarServiciosSeguridad();
            builder.Services.AgregarServiciosVendedor();
            builder.Services.AgregarServiciosCatalogo();
            builder.Services.AgregarServiciosVenta();
            builder.Services.AgregarServiciosSuscripcion();
            builder.Services.AgregarServiciosPago(builder.Configuration);
            builder.Services.AgregarServiciosReporte();

            // Seguridad: la confirmación demo de Izipay nunca puede estar activa en Production.
            var demoIzipay = builder.Configuration.GetValue<bool>("Izipay:PermitirConfirmacionDemo");
            if (builder.Environment.IsProduction() && demoIzipay)
            {
                throw new InvalidOperationException(
                    "Configuración insegura: Izipay:PermitirConfirmacionDemo=true no está permitido en Production.");
            }

            // Controllers + JSON (enums como string, camelCase)
            builder.Services.AddControllers(mvc =>
            {
                // Con <Nullable>enable</Nullable> en los DTOs, ASP.NET marca
                // implícitamente como requeridas todas las propiedades no
                // anotadas con "?". Preferimos controlar la obligatoriedad
                // sólo con [Required] o el tipo (T vs T?), para evitar
                // sorpresas al hacer campos opcionales.
                mvc.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                    options.JsonSerializerOptions.PropertyNamingPolicy =
                        System.Text.Json.JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.Converters.Add(
                        new System.Text.Json.Serialization.JsonStringEnumConverter());
                });

            // Interceptor de ModelState (errores 400 traducidos al espa�ol)
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

            // Límite de intentos de cobro (anti card-testing). Clave = usuario o IP.
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy("pagos", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                      ?? httpContext.Connection.RemoteIpAddress?.ToString()
                                      ?? "anonimo",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            Window = TimeSpan.FromMinutes(1),
                            PermitLimit = 5,
                            QueueLimit = 0,
                        }));
            });

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
            app.UseRateLimiter();

            app.MapControllers();

            app.Run();
        }
    }
}