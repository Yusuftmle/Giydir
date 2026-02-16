using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Giydir.Web.Infrastructure;

public class AuthTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuthTokenHandler> _logger;

    public AuthTokenHandler(IHttpContextAccessor httpContextAccessor, ILogger<AuthTokenHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[AuthTokenHandler] Request: {Method} {Uri}", request.Method, request.RequestUri);
        
        // Token'ı önce cookie'den, sonra session'dan al
        // NOT: Blazor Server'da HttpClient cookie'leri otomatik göndermez, bu yüzden localStorage kullanıyoruz
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                string? token = null;
                
                // Önce cookie'den dene
                if (httpContext.Request.Cookies.ContainsKey("JwtToken"))
                {
                    token = httpContext.Request.Cookies["JwtToken"];
                    if (!string.IsNullOrEmpty(token))
                    {
                        _logger.LogInformation("[AuthTokenHandler] Token cookie'den alındı: {Uri} (Token length: {TokenLength})", request.RequestUri, token.Length);
                    }
                }
                
                // Cookie'de yoksa session'dan dene
                if (string.IsNullOrEmpty(token))
                {
                    var session = httpContext.Session;
                    if (session != null)
                    {
                        await session.LoadAsync(cancellationToken);
                        token = session.GetString("JwtToken");
                        if (!string.IsNullOrEmpty(token))
                        {
                            _logger.LogInformation("[AuthTokenHandler] Token session'dan alındı: {Uri} (Token length: {TokenLength})", request.RequestUri, token.Length);
                        }
                    }
                }
                
                // Header'dan dene (MainLayout localStorage'dan token'ı header'a ekliyor)
                if (string.IsNullOrEmpty(token) && request.Headers.Contains("X-JWT-Token"))
                {
                    token = request.Headers.GetValues("X-JWT-Token").FirstOrDefault();
                    if (!string.IsNullOrEmpty(token))
                    {
                        _logger.LogInformation("[AuthTokenHandler] Token header'dan alındı: {Uri} (Token length: {TokenLength})", request.RequestUri, token.Length);
                    }
                }

                // Eğer token mevcutsa ve Authorization başlığı eklenmemişse, başlığı ekle
                if (!string.IsNullOrEmpty(token) && request.Headers.Authorization == null)
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    _logger.LogInformation("[AuthTokenHandler] Token eklendi: {Uri} (Token length: {TokenLength})", request.RequestUri, token.Length);
                }
                else if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("[AuthTokenHandler] Token bulunamadı (cookie ve session): {Uri}", request.RequestUri);
                }
                else if (request.Headers.Authorization != null)
                {
                    _logger.LogInformation("[AuthTokenHandler] Authorization header zaten var: {Uri}", request.RequestUri);
                }
            }
            else
            {
                _logger.LogWarning("[AuthTokenHandler] HttpContext null: {Uri}", request.RequestUri);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AuthTokenHandler] ERROR: {Message}", ex.Message);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

