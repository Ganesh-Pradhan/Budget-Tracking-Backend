using BudgetTracker.API.Data;
using BudgetTracker.API.DTOs;
using BudgetTracker.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BudgetsController : ControllerBase
{
    private readonly BudgetTrackerDbContext _context;

    public BudgetsController(BudgetTrackerDbContext context)
    {
        _context = context;
    }

    // GET: api/budgets
    // GET: api/budgets
    [HttpGet]
    public async Task<IActionResult> GetBudgets(
        [FromQuery] int? userId,
        [FromQuery] int? year,
        [FromQuery] int? month,
        int page = 1,
        int pageSize = 5)
    {
        if (page < 1)
            page = 1;

        if (pageSize < 1)
            pageSize = 5;

        if (pageSize > 100)
            pageSize = 100;

        var query = _context.Budgets
            .AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(b =>
                b.UserId == userId.Value);
        }

        if (year.HasValue)
        {
            query = query.Where(b =>
                b.BudgetYear == year.Value);
        }

        if (month.HasValue)
        {
            query = query.Where(b =>
                b.BudgetMonth == month.Value);
        }

        var totalRecords = await query.CountAsync();

        var totalPages = (int)Math.Ceiling(
            totalRecords / (double)pageSize);

        var budgets = await query
            .OrderByDescending(b => b.BudgetYear)
            .ThenByDescending(b => b.BudgetMonth)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new PaginatedResult<Budget>
        {
            TotalRecords = totalRecords,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            Data = budgets
        };

        return Ok(result);
    }

    // GET: api/budgets/1
    [HttpGet("{id}")]
    public async Task<IActionResult> GetBudget(int id)
    {
        var budget = await _context.Budgets
            .FirstOrDefaultAsync(b => b.BudgetId == id);

        if (budget == null)
        {
            return NotFound(new { message = "Budget not found." });
        }

        return Ok(budget);
    }

    // POST: api/budgets
    [HttpPost]
    public async Task<IActionResult> CreateBudget(CreateBudgetDto dto)
    {
        var userExists = await _context.Users
            .AnyAsync(u => u.UserId == dto.UserId);

        if (!userExists)
        {
            return BadRequest(new { message = "User not found." });
        }

        var existingBudget = await _context.Budgets
            .AnyAsync(b =>
                b.UserId == dto.UserId &&
                b.BudgetYear == dto.BudgetYear &&
                b.BudgetMonth == dto.BudgetMonth);

        if (existingBudget)
        {
            return BadRequest(new
            {
                message = "A budget already exists for this user and month."
            });
        }

        var budget = new Budget
        {
            UserId = dto.UserId,
            BudgetYear = dto.BudgetYear,
            BudgetMonth = dto.BudgetMonth,
            BudgetAmount = dto.BudgetAmount,
            CreatedDate = DateTime.Now
        };

        _context.Budgets.Add(budget);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetBudget),
            new { id = budget.BudgetId },
            budget
        );
    }

    // PUT: api/budgets/1
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBudget(
        int id,
        UpdateBudgetDto dto)
    {
        var budget = await _context.Budgets
            .FirstOrDefaultAsync(b => b.BudgetId == id);

        if (budget == null)
        {
            return NotFound(new { message = "Budget not found." });
        }

        var existingBudget = await _context.Budgets
            .AnyAsync(b =>
                b.BudgetId != id &&
                b.UserId == budget.UserId &&
                b.BudgetYear == budget.BudgetYear &&
                b.BudgetMonth == dto.BudgetMonth);

        if (existingBudget)
        {
            return BadRequest(new
            {
                message = "A budget already exists for this user and month."
            });
        }

        budget.BudgetMonth = dto.BudgetMonth;
        budget.BudgetAmount = dto.BudgetAmount;
        budget.UpdatedDate = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(budget);
    }

    // DELETE: api/budgets/1
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBudget(int id)
    {
        var budget = await _context.Budgets
            .FirstOrDefaultAsync(b => b.BudgetId == id);

        if (budget == null)
        {
            return NotFound(new { message = "Budget not found." });
        }

        _context.Budgets.Remove(budget);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Budget deleted successfully."
        });
    }



    // GET: api/budgets/summary/total
    [HttpGet("summary/total")]
    public async Task<IActionResult> GetTotalBudget()
    {
        var totalBudget = await _context.Budgets
            .SumAsync(b => b.BudgetAmount);

        return Ok(new
        {
            totalBudget
        });
    }


    // GET: api/budgets/summary/monthly
    [HttpGet("summary/monthly")]
    public async Task<IActionResult> GetMonthlyBudget(
        [FromQuery] int userId,
        [FromQuery] int year,
        [FromQuery] int month)
    {
        var budget = await _context.Budgets
            .FirstOrDefaultAsync(b =>
                b.UserId == userId &&
                b.BudgetYear == year &&
                b.BudgetMonth == month);

        if (budget == null)
        {
            return NotFound(new
            {
                message = "Budget not found for the specified month."
            });
        }

        return Ok(new
        {
            budget.BudgetId,
            budget.UserId,
            budget.BudgetYear,
            budget.BudgetMonth,
            budget.BudgetAmount
        });
    }


    // GET: api/budgets/summary/vs-expense
    [HttpGet("summary/vs-expense")]
    public async Task<IActionResult> GetBudgetVsExpense(
        [FromQuery] int userId,
        [FromQuery] int year,
        [FromQuery] int month)
    {
        var budget = await _context.Budgets
            .FirstOrDefaultAsync(b =>
                b.UserId == userId &&
                b.BudgetYear == year &&
                b.BudgetMonth == month);

        if (budget == null)
        {
            return NotFound(new
            {
                message = "Budget not found for the specified month."
            });
        }

        var actualExpense = await _context.Expenses
            .Where(e =>
                e.UserId == userId &&
                e.ExpenseDate.Year == year &&
                e.ExpenseDate.Month == month)
            .SumAsync(e => e.Amount);

        var remainingAmount = budget.BudgetAmount - actualExpense;

        return Ok(new
        {
            budgetAmount = budget.BudgetAmount,
            actualExpense,
            remainingAmount
        });
    }
}