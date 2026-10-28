using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
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

            // C3: Secretos nunca viven en appsettings.json committeado. En Development
            // se cargan por 'dotnet user-secrets' (automático por UserSecretsId); en
            // Production, por variables de entorno. Si faltan o son un placeholder
            // obvio, la API se niega a arrancar.
            ValidarConfiguracionSeguridad(builder.Configuration, builder.Environment);

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
                    // Sólo la audiencia normal es válida aquí. Los tokens temporales
                    // (2FA pendiente, ticket de registro por correo) se emiten con la
                    // audiencia "<Audience>.temporal" y por lo tanto YA son rechazados
                    // en este punto con 401, antes de llegar a cualquier endpoint
                    // [Authorize]. Ver JwtTokenService.ObtenerAudienciaTemporal.
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    RequireSignedTokens = true,
                    RequireExpirationTime = true,
                    ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                    ClockSkew = TimeSpan.FromSeconds(30)
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = ctx =>
                    {
                        if (ctx.Principal?.HasClaim(c => c.Type == "scope") == true)
                        {
                            ctx.Fail("Este token no puede usarse en endpoints normales.");
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            // Seguridad: cualquier endpoint sin [Authorize]/[AllowAnonymous] explícito
            // queda protegido por defecto (evita el error de MetodoEnvioController: un
            // [AllowAnonymous] a nivel de clase que anulaba los [Authorize] de sus acciones).
            builder.Services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
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

                // Límite global de respaldo: evita que un endpoint sin política propia
                // quede completamente expuesto a flood/DDoS de bajo esfuerzo.
                options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                    PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                        RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
                            factory: _ => new FixedWindowRateLimiterOptions
                            {
                                Window = TimeSpan.FromMinutes(1),
                                PermitLimit = 300,
                                QueueLimit = 0,
                            })));

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

                options.AddPolicy("codigos", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            Window = TimeSpan.FromMinutes(10),
                            PermitLimit = 12,
                            QueueLimit = 0,
                        }));

                options.AddPolicy("cotizar", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            Window = TimeSpan.FromMinutes(1),
                            PermitLimit = 60,
                            QueueLimit = 0,
                        }));

                // A4+M12: políticas específicas para los puntos de entrada de
                // autenticación/contacto, partidas por IP (no hay usuario aún en la
                // mayoría de estos casos).
                options.AddPolicy("login", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            Window = TimeSpan.FromMinutes(1),
                            PermitLimit = 10,
                            QueueLimit = 0,
                        }));

                options.AddPolicy("google", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            Window = TimeSpan.FromMinutes(1),
                            PermitLimit = 15,
                            QueueLimit = 0,
                        }));

                options.AddPolicy("registro", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            Window = TimeSpan.FromMinutes(10),
                            PermitLimit = 8,
                            QueueLimit = 0,
                        }));

                options.AddPolicy("refrescar", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            Window = TimeSpan.FromMinutes(1),
                            PermitLimit = 20,
                            QueueLimit = 0,
                        }));

                options.AddPolicy("contacto", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            Window = TimeSpan.FromMinutes(10),
                            PermitLimit = 5,
                            QueueLimit = 0,
                        }));

                options.AddPolicy("dosFactores", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            Window = TimeSpan.FromMinutes(10),
                            PermitLimit = 15,
                            QueueLimit = 0,
                        }));

                options.AddPolicy("media", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                      ?? httpContext.Connection.RemoteIpAddress?.ToString()
                                      ?? "anonimo",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            Window = TimeSpan.FromMinutes(1),
                            PermitLimit = 30,
                            QueueLimit = 0,
                        }));
            });

            if (builder.Configuration.GetValue<bool>("ForwardedHeaders:Habilitado"))
            {
                builder.Services.Configure<ForwardedHeadersOptions>(opciones =>
                {
                    opciones.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                        | ForwardedHeaders.XForwardedProto;
                    opciones.KnownNetworks.Clear();
                    opciones.KnownProxies.Clear();
                });
            }

            // Build & Pipeline
            var app = builder.Build();

            if (app.Configuration.GetValue<bool>("ForwardedHeaders:Habilitado"))
                app.UseForwardedHeaders();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseCors("MisOrigenesPermitidos");

            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
                app.UseHsts();
            }

            app.Use(async (ctx, next) =>
            {
                var headers = ctx.Response.Headers;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
                await next();
            });

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseRateLimiter();

            app.MapControllers();

            app.Run();
        }

        /// <summary>
        /// C3: valida que los secretos críticos no sean un placeholder ni estén vacíos.
        /// En Production, cualquier falla aquí detiene el arranque (preferible a correr
        /// insegura). En Development sólo se exige que no esté vacío, para no bloquear
        /// a un desarrollador que use un valor de prueba corto.
        /// </summary>
        private static void ValidarConfiguracionSeguridad(
            IConfiguration configuration, IWebHostEnvironment environment)
        {
            var jwtKey = configuration["Jwt:Key"] ?? string.Empty;
            var claveCifrado2Fa = configuration["TwoFactor:ClaveCifrado"] ?? string.Empty;
            var connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;

            if (environment.IsProduction())
            {
                var errores = new List<string>();

                if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Contains("CAMBIA", StringComparison.OrdinalIgnoreCase))
                    errores.Add("Jwt:Key no está configurado o sigue siendo el placeholder de ejemplo.");
                else if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
                    errores.Add("Jwt:Key debe tener al menos 32 bytes (256 bits).");

                if (string.IsNullOrWhiteSpace(claveCifrado2Fa))
                    errores.Add("TwoFactor:ClaveCifrado no está configurado.");

                if (string.IsNullOrWhiteSpace(connectionString))
                    errores.Add("ConnectionStrings:DefaultConnection no está configurado.");
                else if (connectionString.Contains("Include Error Detail=true", StringComparison.OrdinalIgnoreCase))
                    errores.Add("ConnectionStrings:DefaultConnection no debe usar Include Error Detail=true en Production (filtra detalles internos en errores).");

                if (errores.Count > 0)
                {
                    throw new InvalidOperationException(
                        "Configuración insegura para Production:\n - " + string.Join("\n - ", errores));
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(jwtKey))
                    throw new InvalidOperationException(
                        "Jwt:Key no está configurado. Configúralo con 'dotnet user-secrets set \"Jwt:Key\" \"...\"' " +
                        "dentro de TiendaVirtual.Api.");

                if (string.IsNullOrWhiteSpace(claveCifrado2Fa))
                    throw new InvalidOperationException(
                        "TwoFactor:ClaveCifrado no está configurado. Configúralo con " +
                        "'dotnet user-secrets set \"TwoFactor:ClaveCifrado\" \"...\"' dentro de TiendaVirtual.Api.");

                if (string.IsNullOrWhiteSpace(connectionString))
                    throw new InvalidOperationException(
                        "ConnectionStrings:DefaultConnection no está configurado. Configúralo con " +
                        "'dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"...\"' dentro de TiendaVirtual.Api.");
            }
        }
    }
}