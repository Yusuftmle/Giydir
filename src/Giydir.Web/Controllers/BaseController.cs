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
            var userIdClaim = User.FindFirst("sub")?.Value;
            if (int.TryParse(userIdClaim, out var userId))
                return userId;
            return null;
        }
    }

    protected string? UserEmail => User.FindFirst("email")?.Value;
    protected string? UserRole => User.FindFirst("role")?.Value;
}
