using System.Security.Claims;

namespace Giydir.Core.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(IEnumerable<Claim> claims);
    ClaimsPrincipal? ValidateToken(string token);
}

