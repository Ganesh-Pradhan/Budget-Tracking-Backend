using BudgetTracker.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ApiControllerBase
{
    private readonly BudgetTrackerDbContext _context;

    public DashboardController(BudgetTrackerDbContext context)
    {
        _context = context;
    }

    // GET: api/dashboard/summary
    // Always scoped to the authenticated user's ID from the validated JWT.
    [HttpGet("summary")]
    public async Task<IActionResult> GetDashboardSummary()
    {
        var authError = RequireAuth();
        if (authError != null) return authError;

        var userId = CurrentUserId.Value;

        var totalIncome = await _context.Income
            .Where(i => i.UserId == userId)
            .SumAsync(i => i.Amount);

        var totalExpenses = await _context.Expenses
            .Where(e => e.UserId == userId)
            .SumAsync(e => e.Amount);

        var balance = totalIncome - totalExpenses;

        return Ok(new
        {
            totalIncome,
            totalExpenses,
            balance
        });
    }




    // GET: api/dashboard/monthly-summary
    [HttpGet("monthly-summary")]
    public async Task<IActionResult> GetMonthlySummary(
    [FromQuery] int year)
    {
        var authError = RequireAuth();
        if (authError != null) return authError;

        var userId = CurrentUserId.Value;

        var incomeQuery = _context.Income
            .Where(i => i.UserId == userId && i.IncomeDate.Year == year);

        var expenseQuery = _context.Expenses
            .Where(e => e.UserId == userId && e.ExpenseDate.Year == year);

        var income = await incomeQuery
            .GroupBy(i => i.IncomeDate.Month)
            .Select(g => new
            {
                month = g.Key,
                income = g.Sum(i => i.Amount)
            })
            .ToListAsync();

        var expenses = await expenseQuery
            .GroupBy(e => e.ExpenseDate.Month)
            .Select(g => new
            {
                month = g.Key,
                expense = g.Sum(e => e.Amount)
            })
            .ToListAsync();

        var monthlySummary = Enumerable.Range(1, 12)
            .Select(month =>
            {
                var monthlyIncome = income
                    .FirstOrDefault(i => i.month == month)?.income ?? 0;

                var monthlyExpense = expenses
                    .FirstOrDefault(e => e.month == month)?.expense ?? 0;

                return new
                {
                    month,
                    monthName = new DateTime(year, month, 1).ToString("MMMM"),
                    income = monthlyIncome,
                    expense = monthlyExpense,
                    balance = monthlyIncome - monthlyExpense
                };
            })
            .ToList();

        return Ok(monthlySummary);
    }
    // GET: api/dashboard/recent-transactions
    [HttpGet("recent-transactions")]
    public async Task<IActionResult> GetRecentTransactions()
    {
        var authError = RequireAuth();
        if (authError != null) return authError;

        var userId = CurrentUserId.Value;

        var transactions = await _context.Transactions
            .Include(t => t.Category)
            .Include(t => t.PaymentMode)
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.TransactionId)
            .Take(5)
            .ToListAsync();

        return Ok(transactions);
    }


    // GET: api/dashboard/expense-by-category
    [HttpGet("expense-by-category")]
    public async Task<IActionResult> GetExpenseByCategory()
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
                totalExpense = g.Sum(e => e.Amount)
            })
            .OrderByDescending(x => x.totalExpense)
            .ToListAsync();

        return Ok(expensesByCategory);
    }
}