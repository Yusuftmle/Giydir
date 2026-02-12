using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Giydir.Web.Controllers;

[ApiController]
public class BaseController : ControllerBase
{
    protected int? UserId
    {
        get
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
                return userId;
            return null;
        }
    }

    protected string? UserEmail => User.FindFirst(ClaimTypes.Email)?.Value;
    protected string? UserRole => User.FindFirst(ClaimTypes.Role)?.Value;
}

