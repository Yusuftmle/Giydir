using Giydir.Core.DTOs;
using Giydir.Core.Entities;
using Giydir.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Giydir.Web.Controllers;

[Route("api/[controller]")]
public class AuthController : BaseController
{
    private readonly IAuthService _authService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, IUserRepository userRepository, ILogger<AuthController> logger)
    {
        _authService = authService;
        _userRepository = userRepository;
        _logger = logger;
    }

    private static UserDto MapToUserDto(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        Credits = user.Credits,
        CreatedAt = user.CreatedAt,
        Name = user.Name,
        Title = user.Title,
        BoutiqueName = user.BoutiqueName,
        Sector = user.Sector,
        WebsiteUrl = user.WebsiteUrl,
        Role = user.Role
    };

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto dto)
    {
        var result = await _authService.RegisterAsync(dto.Email, dto.Password, dto.Name);

        if (!result.Success)
            return BadRequest(new AuthResponseDto { Success = false, Error = result.ErrorMessage });

        var token = result.Token ?? _authService.GetToken();
        
        // Token'ı hem session'a hem cookie'ye kaydet (response dönmeden önce!)
        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                // Session'a kaydet
                var session = HttpContext.Session;
                if (session != null)
                {
                    await session.LoadAsync();
                    session.SetString("JwtToken", token);
                    await session.CommitAsync();
                    _logger.LogInformation("[AuthController] Token session'a kaydedildi (Register endpoint)");
                }
                
                // Cookie'ye de kaydet (Blazor Server SignalR için)
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = false, // JavaScript'ten okunabilir olmalı (Blazor Server için)
                    Secure = true, // HTTPS (ngrok) için true olmalı
                    SameSite = SameSiteMode.Lax,
                    Path = "/", // Root path'te erişilebilir olmalı
                    Expires = DateTimeOffset.UtcNow.AddDays(10)
                };
                Response.Cookies.Append("JwtToken", token, cookieOptions);
                _logger.LogInformation("[AuthController] Token cookie'ye kaydedildi (Register endpoint) - Path: {Path}, HttpOnly: {HttpOnly}", cookieOptions.Path, cookieOptions.HttpOnly);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AuthController] Token kaydetme hatası: {Message}", ex.Message);
            }
        }

        return Ok(new AuthResponseDto
        {
            Success = true,
            Token = token,
            User = MapToUserDto(result.User!)
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto.Email, dto.Password);

        if (!result.Success)
            return Unauthorized(new AuthResponseDto { Success = false, Error = result.ErrorMessage });

        var token = result.Token ?? _authService.GetToken();
        
        // Token'ı hem session'a hem cookie'ye kaydet (response dönmeden önce!)
        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                // Session'a kaydet
                var session = HttpContext.Session;
                if (session != null)
                {
                    await session.LoadAsync();
                    session.SetString("JwtToken", token);
                    await session.CommitAsync();
                    _logger.LogInformation("[AuthController] Token session'a kaydedildi (Login endpoint)");
                }
                
                // Cookie'ye de kaydet (Blazor Server SignalR için)
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = false, // JavaScript'ten okunabilir olmalı (Blazor Server için)
                    Secure = true, // HTTPS (ngrok) için true olmalı
                    SameSite = SameSiteMode.Lax,
                    Path = "/", // Root path'te erişilebilir olmalı
                    Expires = DateTimeOffset.UtcNow.AddDays(10)
                };
                Response.Cookies.Append("JwtToken", token, cookieOptions);
                _logger.LogInformation("[AuthController] Token cookie'ye kaydedildi (Login endpoint) - Path: {Path}, HttpOnly: {HttpOnly}", cookieOptions.Path, cookieOptions.HttpOnly);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AuthController] Token kaydetme hatası: {Message}", ex.Message);
            }
        }

        return Ok(new AuthResponseDto
        {
            Success = true,
            Token = token,
            User = MapToUserDto(result.User!)
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync();
        
        // Cookie'yi de temizle
        Response.Cookies.Delete("JwtToken");
        _logger.LogInformation("[AuthController] Token cookie'den silindi (Logout endpoint)");
        
        return Ok(new { message = "Çıkış yapıldı" });
    }

    /// <summary>
    /// GET ile çıkış yapma endpoint'i - SSR modunda tarayıcıdan doğrudan çağrılır
    /// JS tarafında localStorage/cookie zaten temizlendi, burada session temizlenip ana sayfaya yönlendirilir
    /// </summary>
    [HttpGet("signout")]
    public new async Task<IActionResult> SignOut()
    {
        _logger.LogInformation("[AuthController] SignOut (GET) çağrıldı");
        
        await _authService.LogoutAsync();
        
        // Session'dan temizle
        try
        {
            var session = HttpContext.Session;
            if (session != null)
            {
                await session.LoadAsync();
                session.Remove("JwtToken");
                await session.CommitAsync();
                _logger.LogInformation("[AuthController] Token session'dan silindi (SignOut endpoint)");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AuthController] Session temizleme hatası: {Message}", ex.Message);
        }
        
        // Cookie'yi de temizle (JS tarafında zaten temizlendi ama sunucu tarafından da ek güvence)
        Response.Cookies.Delete("JwtToken", new CookieOptions { Path = "/" });
        _logger.LogInformation("[AuthController] Token cookie'den silindi (SignOut endpoint)");
        
        // Ana sayfaya yönlendir
        return Redirect("/");
    }

    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<ActionResult<AuthResponseDto>> GetCurrentUser()
    {
        var user = await _authService.GetCurrentUserAsync();
        if (user == null)
            return Unauthorized(new AuthResponseDto { Success = false, Error = "Oturum açılmamış" });

        return Ok(new AuthResponseDto
        {
            Success = true,
            Token = _authService.GetToken(),
            User = MapToUserDto(user)
        });
    }

    [HttpPut("profile")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<ActionResult<AuthResponseDto>> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var user = await _authService.GetCurrentUserAsync();
        if (user == null)
            return Unauthorized(new AuthResponseDto { Success = false, Error = "Oturum açılmamış" });

        // Profil alanlarını güncelle
        user.Name = dto.Name;
        user.Title = dto.Title;
        user.BoutiqueName = dto.BoutiqueName;
        user.Sector = dto.Sector;
        user.WebsiteUrl = dto.WebsiteUrl;

        await _userRepository.UpdateAsync(user);
        _logger.LogInformation("[AuthController] Profil güncellendi: {UserId}", user.Id);

        return Ok(new AuthResponseDto
        {
            Success = true,
            User = MapToUserDto(user)
        });
    }
}

