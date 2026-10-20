using BudgetTracker.API.Data;
using BudgetTracker.API.DTOs;
using BudgetTracker.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExpensesController : ApiControllerBase
{
    private readonly BudgetTrackerDbContext _context;

    public ExpensesController(BudgetTrackerDbContext context)
    {
        _context = context;
    }

    // GET: api/expenses
    [HttpGet]
    public async Task<IActionResult> GetExpenses(
     [FromQuery] string? search,
     [FromQuery] int? categoryId,
     [FromQuery] int? paymentModeId,
     [FromQuery] int? year,
     [FromQuery] int? month,
     [FromQuery] int page = 1,
     [FromQuery] int pageSize = 5)
    {
        var authError = RequireAuth();
        if (authError != null) return authError;

        if (page < 1)
            page = 1;

        if (pageSize < 1)
            pageSize = 5;

        if (pageSize > 100)
            pageSize = 100;

        var userId = CurrentUserId.Value;
        var query = _context.Expenses
            .Include(e => e.Category)
            .Include(e => e.PaymentMode)
            .Where(e => e.UserId == userId)
            .AsQueryable();

        // Search by description
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e =>
                e.Description != null &&
                e.Description.Contains(search));
        }

        // Filter by category
        if (categoryId.HasValue)
        {
            query = query.Where(e =>
                e.CategoryId == categoryId.Value);
        }

        // Filter by payment mode
        if (paymentModeId.HasValue)
        {
            query = query.Where(e =>
                e.PaymentModeId == paymentModeId.Value);
        }

        // Filter by year
        if (year.HasValue)
        {
            query = query.Where(e =>
                e.ExpenseDate.Year == year.Value);
        }

        // Filter by month
        if (month.HasValue)
        {
            query = query.Where(e =>
                e.ExpenseDate.Month == month.Value);
        }

        var totalRecords = await query.CountAsync();

        var totalPages = (int)Math.Ceiling(
            totalRecords / (double)pageSize);

        var expenses = await query
            .OrderByDescending(e => e.ExpenseDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new PaginatedResult<Expense>
        {
            TotalRecords = totalRecords,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            Data = expenses
        };

        return Ok(result);
    }

    // GET: api/expenses/1
    [HttpGet("{id}")]
    public async Task<IActionResult> GetExpense(int id)
    {
        var authError = RequireAuth();
        if (authError != null) return authError;

        var userId = CurrentUserId.Value;
        var expense = await _context.Expenses
            .Include(e => e.Category)
            .Include(e => e.PaymentMode)
            .FirstOrDefaultAsync(e =>
                e.ExpenseId == id && e.UserId == userId);

        if (expense == null)
        {
            return NotFound(new
            {
                message = "Expense not found."
            });
        }

        return Ok(expense);
    }

    // POST: api/expenses
    [HttpPost]
    public async Task<IActionResult> CreateExpense(CreateExpenseDto dto)
    {
        var authError = RequireAuth();
        if (authError != null) return authError;

        var userId = CurrentUserId.Value;
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId);

        if (category == null)
        {
            return BadRequest(new
            {
                message = "Category not found."
            });
        }

        // 5. A category used for an expense must be an Expense category.
        //    Reject Income categories here so an expense can never reference
        //    an income category.
        if (category.CategoryType != "Expense")
        {
            return BadRequest(new
            {
                message = "Expense transactions can only use Expense categories."
            });
        }

        var paymentMode = await _context.PaymentModes
            .FirstOrDefaultAsync(p => p.PaymentModeId == dto.PaymentModeId);

        if (paymentMode == null)
        {
            return BadRequest(new
            {
                message = "Payment mode not found."
            });
        }

        if (!paymentMode.IsActive)
        {
            return BadRequest(new
            {
                message = "Payment mode is inactive and cannot be used."
            });
        }

        var account = await _context.Accounts
            .FirstOrDefaultAsync(a =>
                a.AccountId == dto.AccountId &&
                a.UserId == userId);

        if (account == null)
        {
            return BadRequest(new
            {
                message = "Account not found."
            });
        }

        if (!account.IsActive)
        {
            return BadRequest(new
            {
                message = "Account is inactive."
            });
        }

        if (account.Balance < dto.Amount)
        {
            return BadRequest(new
            {
                message = "Insufficient account balance."
            });
        }

        var expense = new Expense
        {
            UserId = userId,
            AccountId = dto.AccountId,
            CategoryId = dto.CategoryId,
            PaymentModeId = dto.PaymentModeId,
            Amount = dto.Amount,
            ExpenseDate = dto.ExpenseDate,
            Description = dto.Description,
            CreatedDate = DateTime.Now
        };

        _context.Expenses.Add(expense);

        // Decrease account balance
        account.Balance -= dto.Amount;

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetExpense),
            new { id = expense.ExpenseId },
            expense
        );
    }

    // PUT: api/expenses/1
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateExpense(
        int id,
        UpdateExpenseDto dto)
    {
        var authError = RequireAuth();
        if (authError != null) return authError;

        var userId = CurrentUserId.Value;
        var expense = await _context.Expenses
            .FirstOrDefaultAsync(e =>
                e.ExpenseId == id && e.UserId == userId);

        if (expense == null)
        {
            return NotFound(new
            {
                message = "Expense not found."
            });
        }

        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId);

        if (category == null)
        {
            return BadRequest(new
            {
                message = "Category not found."
            });
        }

        // 5. A category used for an expense must be an Expense category.
        //    Reject Income categories here so an expense can never reference
        //    an income category.
        if (category.CategoryType != "Expense")
        {
            return BadRequest(new
            {
                message = "Expense transactions can only use Expense categories."
            });
        }

        var paymentMode = await _context.PaymentModes
            .FirstOrDefaultAsync(p => p.PaymentModeId == dto.PaymentModeId);

        if (paymentMode == null)
        {
            return BadRequest(new
            {
                message = "Payment mode not found."
            });
        }

        if (!paymentMode.IsActive)
        {
            return BadRequest(new
            {
                message = "Payment mode is inactive and cannot be used."
            });
        }

        var newAccount = await _context.Accounts
            .FirstOrDefaultAsync(a =>
                a.AccountId == dto.AccountId &&
                a.UserId == userId);

        if (newAccount == null)
        {
            return BadRequest(new
            {
                message = "Account not found."
            });
        }

        if (!newAccount.IsActive)
        {
            return BadRequest(new
            {
                message = "Account is inactive."
            });
        }

        var oldAccount = await _context.Accounts
            .FirstOrDefaultAsync(a => a.AccountId == expense.AccountId);

        if (oldAccount == null)
        {
            return BadRequest(new
            {
                message = "Original expense account not found."
            });
        }

        // Restore the old expense amount
        oldAccount.Balance += expense.Amount;

        // Check balance for the new expense amount
        if (newAccount.Balance < dto.Amount)
        {
            return BadRequest(new
            {
                message = "Insufficient account balance."
            });
        }

        // Apply the new expense amount
        newAccount.Balance -= dto.Amount;

        expense.AccountId = dto.AccountId;
        expense.CategoryId = dto.CategoryId;
        expense.PaymentModeId = dto.PaymentModeId;
        expense.Amount = dto.Amount;
        expense.ExpenseDate = dto.ExpenseDate;
        expense.Description = dto.Description;

        await _context.SaveChangesAsync();

        return Ok(expense);
    }

    // DELETE: api/expenses/1
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteExpense(int id)
    {
        var authError = RequireAuth();
        if (authError != null) return authError;

        var userId = CurrentUserId.Value;
        var expense = await _context.Expenses
            .FirstOrDefaultAsync(e =>
                e.ExpenseId == id && e.UserId == userId);

        if (expense == null)
        {
            return NotFound(new
            {
                message = "Expense not found."
            });
        }

        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.AccountId == expense.AccountId);

        if (account == null)
        {
            return BadRequest(new
            {
                message = "Expense account not found."
            });
        }

        // Restore the expense amount
        account.Balance += expense.Amount;

        _context.Expenses.Remove(expense);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Expense deleted successfully."
        });
    }


    // GET: api/expenses/summary/total
    [HttpGet("summary/total")]
    public async Task<IActionResult> GetTotalExpenses()
    {
        var authError = RequireAuth();
        if (authError != null) return authError;

        var userId = CurrentUserId.Value;
        var totalExpenses = await _context.Expenses
            .Where(e => e.UserId == userId)
            .SumAsync(e => e.Amount);

        return Ok(new { totalExpenses });
    }

    // GET: api/expenses/summary/monthly
    [HttpGet("summary/monthly")]
    public async Task<IActionResult> GetMonthlyExpenses(
        [FromQuery] int year,
        [FromQuery] int month)
    {
        var authError = RequireAuth();
        if (authError != null) return authError;

        var userId = CurrentUserId.Value;
        var totalExpenses = await _context.Expenses
            .Where(e =>
                e.UserId == userId &&
                e.ExpenseDate.Year == year &&
                e.ExpenseDate.Month == month)
            .SumAsync(e => e.Amount);

        return Ok(new
        {
            year,
            month,
            totalExpenses
        });
    }

    // GET: api/expenses/summary/by-category
    [HttpGet("summary/by-category")]
    public async Task<IActionResult> GetExpensesByCategory()
    {
        var authError = RequireAuth();
        if (authError != null) return authError;

        var userId = CurrentUserId.Value;
        var expensesByCategory = await _context.Expenses
            .Include(e => e.Category)
            .Where(e => e.UserId == userId)
            .GroupBy(e => new
            {
                e.CategoryId,
                CategoryName = e.Category != null
                    ? e.Category.CategoryName
                    : "Unknown"
            })
            .Select(g => new
            {
                categoryId = g.Key.CategoryId,
                categoryName = g.Key.CategoryName,
                totalExpenses = g.Sum(e => e.Amount)
            })
            .OrderByDescending(x => x.totalExpenses)
            .ToListAsync();

        return Ok(expensesByCategory);
    }
}