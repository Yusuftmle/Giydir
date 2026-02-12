using System.Security.Claims;
using Giydir.Core.Entities;
using Giydir.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Giydir.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthService> _logger;

    private const string SessionUserIdKey = "UserId";
    private string? _currentToken;

    public AuthService(
        IUserRepository userRepository,
        IHttpContextAccessor httpContextAccessor,
        IJwtTokenService jwtTokenService,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _httpContextAccessor = httpContextAccessor;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<AuthResult> RegisterAsync(string email, string password, string? name = null)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return AuthResult.Fail("E-posta ve şifre gereklidir.");

        if (password.Length < 6)
            return AuthResult.Fail("Şifre en az 6 karakter olmalıdır.");

        var existingUser = await _userRepository.GetByEmailAsync(email);
        if (existingUser != null)
            return AuthResult.Fail("Bu e-posta adresi zaten kayıtlı.");

        var user = new User
        {
            Email = email.Trim().ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Credits = 50, // Hoş geldin kredisi
            Name = name,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.CreateAsync(user);

        // JWT Token oluştur
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, "User")
        };
        _currentToken = _jwtTokenService.GenerateToken(claims);

        // Session'a kullanıcı ID'sini yaz (geriye dönük uyumluluk için)
        SetSession(user.Id);

        _logger.LogInformation("Yeni kullanıcı kaydoldu: {Email}, ID: {UserId}", email, user.Id);

        return AuthResult.Ok(user, _currentToken);
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return AuthResult.Fail("E-posta ve şifre gereklidir.");

        var user = await _userRepository.GetByEmailAsync(email.Trim().ToLower());
        if (user == null)
            return AuthResult.Fail("E-posta veya şifre hatalı.");

        // BCrypt ile hash kontrolü - eski düz metin şifreler için de kontrol
        bool passwordValid;
        if (user.PasswordHash.StartsWith("$2"))
        {
            passwordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }
        else
        {
            // Eski düz metin şifre (migration için)
            passwordValid = user.PasswordHash == password;
            if (passwordValid)
            {
                // Şifreyi hash'le ve güncelle
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                await _userRepository.UpdateAsync(user);
            }
        }

        if (!passwordValid)
            return AuthResult.Fail("E-posta veya şifre hatalı.");

        // JWT Token oluştur
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, "User")
        };
        _currentToken = _jwtTokenService.GenerateToken(claims);

        // Session'a kullanıcı ID'sini yaz (geriye dönük uyumluluk için)
        SetSession(user.Id);

        _logger.LogInformation("Kullanıcı giriş yaptı: {Email}, ID: {UserId}", email, user.Id);

        return AuthResult.Ok(user, _currentToken);
    }

    public Task LogoutAsync()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        session?.Remove(SessionUserIdKey);
        _currentToken = null; // Token'ı temizle
        _logger.LogInformation("Kullanıcı çıkış yaptı");
        return Task.CompletedTask;
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return null;

        return await _userRepository.GetByIdAsync(userId.Value);
    }

    public int? GetCurrentUserId()
    {
        // Önce JWT token'dan dene (daha güvenli)
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
                return userId;
        }

        // Fallback: Session'dan oku
        var session = httpContext?.Session;
        var userIdStr = session?.GetInt32(SessionUserIdKey);
        return userIdStr;
    }

    public string? GetToken()
    {
        return _currentToken;
    }

    private void SetSession(int userId)
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        session?.SetInt32(SessionUserIdKey, userId);
    }
}

