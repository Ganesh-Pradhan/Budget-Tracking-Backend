using BudgetTracker.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly BudgetTrackerDbContext _context;

    public DashboardController(BudgetTrackerDbContext context)
    {
        _context = context;
    }

    // GET: api/dashboard/summary
    [HttpGet("summary")]
    public async Task<IActionResult> GetDashboardSummary(
        [FromQuery] int? userId)
    {
        var incomeQuery = _context.Income.AsQueryable();
        var expenseQuery = _context.Expenses.AsQueryable();

        if (userId.HasValue)
        {
            incomeQuery = incomeQuery
                .Where(i => i.UserId == userId.Value);

            expenseQuery = expenseQuery
                .Where(e => e.UserId == userId.Value);
        }

        var totalIncome = await incomeQuery
            .SumAsync(i => i.Amount);

        var totalExpenses = await expenseQuery
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
    [FromQuery] int year,
    [FromQuery] int? userId)
    {
        var incomeQuery = _context.Income
            .Where(i => i.IncomeDate.Year == year);

        var expenseQuery = _context.Expenses
            .Where(e => e.ExpenseDate.Year == year);

        if (userId.HasValue)
        {
            incomeQuery = incomeQuery
                .Where(i => i.UserId == userId.Value);

            expenseQuery = expenseQuery
                .Where(e => e.UserId == userId.Value);
        }

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
    public async Task<IActionResult> GetRecentTransactions(
    [FromQuery] int? userId)
    {
        var query = _context.Transactions
            .Include(t => t.Category)
            .Include(t => t.PaymentMode)
            .AsQueryable();

        if (userId.HasValue)
        {
            query = query
                .Where(t => t.UserId == userId.Value);
        }

        var transactions = await query
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.TransactionId)
            .Take(5)
            .ToListAsync();

        return Ok(transactions);
    }


    // GET: api/dashboard/expense-by-category
    [HttpGet("expense-by-category")]
    public async Task<IActionResult> GetExpenseByCategory(
    [FromQuery] int? userId)
    {
        var query = _context.Expenses
            .Include(e => e.Category)
            .AsQueryable();

        if (userId.HasValue)
        {
            query = query
                .Where(e => e.UserId == userId.Value);
        }

        var expensesByCategory = await query
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