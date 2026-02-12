using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Giydir.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Giydir.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly SymmetricSecurityKey _signingKey;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
        var secret = configuration["AuthConfig:Secret"] 
            ?? throw new InvalidOperationException("AuthConfig:Secret bulunamadı!");
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
    }

    public string GenerateToken(IEnumerable<Claim> claims)
    {
        var creds = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.UtcNow.AddDays(10); // 10 gün geçerli

        var token = new JwtSecurityToken(
            claims: claims,
            expires: expiry,
            signingCredentials: creds,
            notBefore: DateTime.UtcNow
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _signingKey
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }
}

