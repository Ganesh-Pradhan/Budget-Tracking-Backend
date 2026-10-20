using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BudgetTracker.API.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Gets the authenticated user's ID from the JWT claims.
    /// Returns null if the user is not authenticated.
    /// </summary>
    protected int? CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    /// <summary>
    /// Requires an authenticated user. Returns Unauthorized if not authenticated.
    /// </summary>
    protected IActionResult? RequireAuth()
    {
        if (!CurrentUserId.HasValue)
            return Unauthorized(new { message = "Authentication required." });
        return null;
    }
}
