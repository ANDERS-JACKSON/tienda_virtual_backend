using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TiendaVirtual.Dominio.Servicios.Sistema.Implementacion
{
    public class JwtTokenService
    {
        private readonly IConfiguration _configuration;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Genera un access token JWT con los datos del usuario y sus roles.
        /// Si el usuario es vendedor, incluye también su vendedorId en los claims.
        /// </summary>
        public (string Token, DateTime ExpiraEn) GenerarToken(
            int usuarioId,
            string correo,
            int personaId,
            List<string> roles,
            int? vendedorId = null,
            int duracionMinutos = 60)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));

            var claims = new List<Claim>
            {
                // Claims estándar de JWT
                new Claim(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, correo),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

                // Claims personalizados
                new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()),
                new Claim("personaId", personaId.ToString()),
                new Claim("correo", correo)
            };

            // Roles del usuario (ADMIN, CLIENTE, VENDEDOR, VERIFICADOR)
            foreach (var rol in roles)
                claims.Add(new Claim(ClaimTypes.Role, rol));

            // Si es vendedor, incluir su ID para filtrar productos/órdenes en las queries
            if (vendedorId.HasValue)
                claims.Add(new Claim("vendedorId", vendedorId.Value.ToString()));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiraEn = DateTime.UtcNow.AddMinutes(duracionMinutos);

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiraEn,
                signingCredentials: creds
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), expiraEn);
        }

        /// <summary>
        /// Genera un refresh token aleatorio criptográficamente seguro.
        /// El valor en plano se devuelve al cliente; el HASH es lo que se guarda en BD.
        /// </summary>
        public string GenerarRefreshToken()
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Calcula el SHA-256 del refresh token para guardarlo en BD.
        /// Así, aunque alguien acceda a la tabla token_refresco, no puede usar los tokens.
        /// </summary>
        public string HashearRefreshToken(string token)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }


        /// <summary>
        /// Token temporal corto (10 min) que solo sirve para validar 2FA.
        /// Si traes setupSecret, indica que es el primer enrolamiento.
        /// </summary>
        public string GenerarTokenTemporal2Fa(int usuarioId, string? setupSecret = null, int duracionMinutos = 10)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));

            var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()),
        new Claim("scope", "2fa_pending")
    };

            if (!string.IsNullOrEmpty(setupSecret))
                claims.Add(new Claim("setupSecret", setupSecret));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(duracionMinutos),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Valida un token y devuelve sus claims. Devuelve null si es inválido o expiró.
        /// </summary>
        public ClaimsPrincipal? ValidarToken(string token)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));

            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                return new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _);
            }
            catch { return null; }
        }
    }
}
