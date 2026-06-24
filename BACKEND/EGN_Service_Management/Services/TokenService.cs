using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IServices;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace EGN_Service_Management.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;

        public TokenService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateToken(Account account, string activeRole)
        {
            var keyString = _config["JwtSettings:Key"] ?? "SUPER_SECRET_KEY_DO_NOT_SHARE_WITH_ANYONE_1234567890";
            var issuer = _config["JwtSettings:Issuer"] ?? "EgnServiceManagementSystem";
            var audience = _config["JwtSettings:Audience"] ?? "EgnServiceManagementClients";
            var expiryDays = double.TryParse(_config["JwtSettings:ExpiryInDays"], out var days) ? days : 7;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.NameId, account.Nic),
                new Claim(ClaimTypes.Role, activeRole),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            if (!string.IsNullOrEmpty(account.Email))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.Email, account.Email));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(expiryDays),
                SigningCredentials = creds,
                Issuer = issuer,
                Audience = audience
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}
