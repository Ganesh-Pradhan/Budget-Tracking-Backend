using BudgetTracker.API.Data;
using BudgetTracker.API.DTOs;
using BudgetTracker.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly BudgetTrackerDbContext _context;

    public ExpensesController(BudgetTrackerDbContext context)
    {
        _context = context;
    }

    // GET: api/expenses
    [HttpGet]
    public async Task<IActionResult> GetExpenses(
     [FromQuery] int? userId,
     [FromQuery] string? search,
     [FromQuery] int? categoryId,
     [FromQuery] int? paymentModeId,
     [FromQuery] int? year,
     [FromQuery] int? month,
     [FromQuery] int page = 1,
     [FromQuery] int pageSize = 5)
    {
        if (page < 1)
            page = 1;

        if (pageSize < 1)
            pageSize = 5;

        if (pageSize > 100)
            pageSize = 100;

        var query = _context.Expenses
            .Include(e => e.Category)
            .Include(e => e.PaymentMode)
            .AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(x => x.UserId == userId.Value);
        }

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
        var expense = await _context.Expenses
            .Include(e => e.Category)
            .Include(e => e.PaymentMode)
            .FirstOrDefaultAsync(e => e.ExpenseId == id);

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
        var categoryExists = await _context.Categories
            .AnyAsync(c => c.CategoryId == dto.CategoryId);

        if (!categoryExists)
        {
            return BadRequest(new
            {
                message = "Category not found."
            });
        }

        var paymentModeExists = await _context.PaymentModes
            .AnyAsync(p => p.PaymentModeId == dto.PaymentModeId);

        if (!paymentModeExists)
        {
            return BadRequest(new
            {
                message = "Payment mode not found."
            });
        }

        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.AccountId == dto.AccountId);

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

        // Check account ownership
        if (account.UserId != dto.UserId)
        {
            return BadRequest(new
            {
                message = "Account does not belong to the user."
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
            UserId = dto.UserId,
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
        var expense = await _context.Expenses
            .FirstOrDefaultAsync(e => e.ExpenseId == id);

        if (expense == null)
        {
            return NotFound(new
            {
                message = "Expense not found."
            });
        }

        var categoryExists = await _context.Categories
            .AnyAsync(c => c.CategoryId == dto.CategoryId);

        if (!categoryExists)
        {
            return BadRequest(new
            {
                message = "Category not found."
            });
        }

        var paymentModeExists = await _context.PaymentModes
            .AnyAsync(p => p.PaymentModeId == dto.PaymentModeId);

        if (!paymentModeExists)
        {
            return BadRequest(new
            {
                message = "Payment mode not found."
            });
        }

        var newAccount = await _context.Accounts
            .FirstOrDefaultAsync(a => a.AccountId == dto.AccountId);

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

        if (newAccount.UserId != expense.UserId)
        {
            return BadRequest(new
            {
                message = "Account does not belong to the user."
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
        var expense = await _context.Expenses
            .FirstOrDefaultAsync(e => e.ExpenseId == id);

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
        var totalExpenses = await _context.Expenses
            .SumAsync(e => e.Amount);

        return Ok(new { totalExpenses });
    }

    // GET: api/expenses/summary/monthly
    [HttpGet("summary/monthly")]
    public async Task<IActionResult> GetMonthlyExpenses(
        [FromQuery] int year,
        [FromQuery] int month)
    {
        var totalExpenses = await _context.Expenses
            .Where(e =>
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
        var expensesByCategory = await _context.Expenses
            .Include(e => e.Category)
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