using EMS.Application.Interfaces;
using EMS.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EMS.Infrastructure.Services
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _config;

        public JwtTokenService(IConfiguration config)
        {
            JwtKeyValidator.EnsureValid(config["Jwt:Key"]);
            _config = config;
        }

        public string GenerateAccessToken(User user)
        {
            var key = _config["Jwt:Key"]!;
            var issuer = _config["Jwt:Issuer"] ?? "ems";
            var expires = DateTime.UtcNow.AddMinutes(15);

            var claims = new System.Collections.Generic.List<Claim> {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, user.Role?.Name ?? "Employee")
            };

            // Absent only for SuperAdmin-role users, who sit above every tenant. A bare custom
            // claim type (not a ClaimTypes.* constant) so it survives the JwtSecurityTokenHandler's
            // default inbound claim map untouched, the same way ClaimTypes.NameIdentifier already
            // round-trips today via Sub.
            if (user.CompanyId.HasValue)
                claims.Add(new Claim("company_id", user.CompanyId.Value.ToString()));

            var signing = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var creds = new SigningCredentials(signing, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(issuer, issuer, claims, expires: expires, signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
