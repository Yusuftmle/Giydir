using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Giydir.Web.Infrastructure;

public class AuthStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuthStateProvider> _logger;

    public AuthStateProvider(IHttpContextAccessor httpContextAccessor, ILogger<AuthStateProvider> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        _logger.LogInformation("[AuthStateProvider] GetAuthenticationStateAsync çağrıldı");
        
        // Session'dan oku (Blazor Server'da her zaman session'dan oku)
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("[AuthStateProvider] HttpContext null!");
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var session = httpContext.Session;
        if (session == null)
        {
            _logger.LogWarning("[AuthStateProvider] Session null!");
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        try
        {
            string? token = null;
            string source = "YOK";
            
            // Önce cookie'den dene
            if (httpContext.Request.Cookies.ContainsKey("JwtToken"))
            {
                token = httpContext.Request.Cookies["JwtToken"];
                if (!string.IsNullOrEmpty(token))
                {
                    source = "COOKIE";
                    _logger.LogInformation("[AuthStateProvider] Token cookie'den okundu: {TokenLength} karakter", token.Length);
                }
            }
            
            // Cookie'de yoksa session'dan dene
            if (string.IsNullOrEmpty(token))
            {
                await session.LoadAsync();
                token = session.GetString("JwtToken");
                if (!string.IsNullOrEmpty(token))
                {
                    source = "SESSION";
                    _logger.LogInformation("[AuthStateProvider] Token session'dan okundu: {TokenLength} karakter", token.Length);
                }
            }
            
            // NOT: localStorage'dan token okuma işlemi MainLayout'ta yapılıyor ve HttpClient header'ına ekleniyor
            // AuthStateProvider sadece cookie ve session'dan okuyor
            
            // Debug: Tüm cookie'leri listele
            _logger.LogInformation("[AuthStateProvider] Mevcut cookie'ler: {Cookies}", 
                string.Join(", ", httpContext.Request.Cookies.Keys));
            
            _logger.LogInformation("[AuthStateProvider] Token okundu: {TokenStatus} (Kaynak: {Source})", 
                string.IsNullOrEmpty(token) ? "YOK" : $"VAR ({token.Length} karakter)",
                source);
            
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogInformation("[AuthStateProvider] Token yok, anonymous state döndürülüyor");
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            // Token'ı parse et
            var authState = CreateAuthStateFromToken(token);
            _logger.LogInformation("[AuthStateProvider] Token parse edildi, authenticated: {IsAuthenticated}", authState.User.Identity?.IsAuthenticated);
            return authState;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AuthStateProvider] ERROR: {Message}", ex.Message);
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    private AuthenticationState CreateAuthStateFromToken(string token)
    {
        try
        {
            _logger.LogInformation("[AuthStateProvider] Token parse ediliyor...");
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.ReadJwtToken(token);

            var claims = securityToken.Claims;
            var userId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            
            _logger.LogInformation("[AuthStateProvider] Token parse edildi - UserId: {UserId}, Email: {Email}", userId, email);
            
            var identity = new ClaimsIdentity(claims, "jwt");
            var principal = new ClaimsPrincipal(identity);

            return new AuthenticationState(principal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AuthStateProvider] Token parse ERROR: {Message}", ex.Message);
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    public void NotifyAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task NotifyUserLoginAsync(string token)
    {
        _logger.LogInformation("[AuthStateProvider] NotifyUserLoginAsync çağrıldı - Token length: {TokenLength}", token?.Length ?? 0);
        
        // Token zaten controller'da session'a kaydedildi, sadece state'i güncelle
        // (Response başladıktan sonra session'a yazamayız, bu yüzden controller'da yapıldı)
        
        // State'i güncelle
        _logger.LogInformation("[AuthStateProvider] Authentication state güncelleniyor...");
        var authState = CreateAuthStateFromToken(token);
        NotifyAuthenticationStateChanged(Task.FromResult(authState));
        _logger.LogInformation("[AuthStateProvider] Authentication state güncellendi ve bildirim gönderildi");
    }

    public async Task NotifyUserLogoutAsync()
    {
        _logger.LogInformation("[AuthStateProvider] NotifyUserLogoutAsync çağrıldı");
        
        // Session'dan temizle
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var session = httpContext.Session;
            if (session != null)
            {
                try
                {
                    await session.LoadAsync();
                    session.Remove("JwtToken");
                    await session.CommitAsync();
                    _logger.LogInformation("[AuthStateProvider] Token session'dan silindi");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[AuthStateProvider] Session remove ERROR: {Message}", ex.Message);
                }
            }
        }

        var authState = Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
        NotifyAuthenticationStateChanged(authState);
        _logger.LogInformation("[AuthStateProvider] Logout state güncellendi");
    }
}

