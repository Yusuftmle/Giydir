using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Giydir.Web.Infrastructure;

public static class AuthRegistration
{
    public static IServiceCollection ConfigureAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var secret = configuration["AuthConfig:Secret"] 
            ?? throw new InvalidOperationException("AuthConfig:Secret bulunamadı! appsettings.json'a ekleyin.");
        
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero, // Token expiration için tolerans süresi
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey
            };
        });

        services.AddAuthorization();

        return services;
    }
}

