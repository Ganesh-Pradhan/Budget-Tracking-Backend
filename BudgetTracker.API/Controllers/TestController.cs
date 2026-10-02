using BudgetTracker.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly BudgetTrackerDbContext _context;

    public TestController(BudgetTrackerDbContext context)
    {
        _context = context;
    }

    [HttpGet("database")]
    public async Task<IActionResult> TestDatabase()
    {
        var users = await _context.Users.ToListAsync();

        return Ok(users);
    }
}