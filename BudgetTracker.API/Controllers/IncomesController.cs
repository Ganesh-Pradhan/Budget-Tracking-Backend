using BudgetTracker.API.Data;
using BudgetTracker.API.DTOs;
using BudgetTracker.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.API.Controllers;

[ApiController]
[Route("api/incomes")]
public class IncomesController : ControllerBase
{
    private readonly BudgetTrackerDbContext _context;

    public IncomesController(BudgetTrackerDbContext context)
    {
        _context = context;
    }

    // GET: api/incomes
    [HttpGet]
    public async Task<IActionResult> GetIncomes(
      [FromQuery] int? userId,
     [FromQuery] string? search,
      [FromQuery] int? sourceId,
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

        var query = _context.Income
            .Include(i => i.Source)
            .AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(x => x.UserId == userId.Value);
        }

        // Search by description
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(i =>
                i.Description != null &&
                i.Description.Contains(search));
        }

        // Filter by income source
        if (sourceId.HasValue)
        {
            query = query.Where(i =>
                i.SourceId == sourceId.Value);
        }

        // Filter by year
        if (year.HasValue)
        {
            query = query.Where(i =>
                i.IncomeDate.Year == year.Value);
        }

        // Filter by month
        if (month.HasValue)
        {
            query = query.Where(i =>
                i.IncomeDate.Month == month.Value);
        }

        var totalRecords = await query.CountAsync();

        var totalPages = (int)Math.Ceiling(
            totalRecords / (double)pageSize);

        var incomes = await query
            .OrderByDescending(i => i.IncomeDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new PaginatedResult<Income>
        {
            TotalRecords = totalRecords,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            Data = incomes
        };

        return Ok(result);
    }

    // GET: api/incomes/1
    [HttpGet("{id}")]
    public async Task<IActionResult> GetIncome(int id)
    {
        var income = await _context.Income
            .Include(i => i.Source)
            .FirstOrDefaultAsync(i => i.IncomeId == id);

        if (income == null)
        {
            return NotFound(new
            {
                message = "Income not found."
            });
        }

        return Ok(income);
    }

    // POST: api/incomes
    [HttpPost]
    public async Task<IActionResult> CreateIncome(CreateIncomeDto dto)
    {
        var sourceExists = await _context.IncomeSources
            .AnyAsync(s => s.SourceId == dto.SourceId);

        if (!sourceExists)
        {
            return BadRequest(new
            {
                message = "Income source not found."
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

        var income = new Income
        {
            UserId = dto.UserId,
            AccountId = dto.AccountId,
            SourceId = dto.SourceId,
            Amount = dto.Amount,
            IncomeDate = dto.IncomeDate,
            Description = dto.Description,
            CreatedDate = DateTime.Now
        };

        _context.Income.Add(income);

        account.Balance += dto.Amount;

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetIncome),
            new { id = income.IncomeId },
            income
        );
    }

    // PUT: api/incomes/1
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateIncome(
      int id,
      UpdateIncomeDto dto)
    {
        var income = await _context.Income
            .FirstOrDefaultAsync(i => i.IncomeId == id);

        if (income == null)
        {
            return NotFound(new
            {
                message = "Income not found."
            });
        }


        var sourceExists = await _context.IncomeSources
            .AnyAsync(s => s.SourceId == dto.SourceId);

        if (!sourceExists)
        {
            return BadRequest(new
            {
                message = "Income source not found."
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

        if (newAccount.UserId != income.UserId)
        {
            return BadRequest(new
            {
                message = "Account does not belong to the user."
            });
        }


        var oldAccount = await _context.Accounts
            .FirstOrDefaultAsync(a => a.AccountId == income.AccountId);

        if (oldAccount == null)
        {
            return BadRequest(new
            {
                message = "Original income account not found."
            });
        }

        // Reverse the old income effect
        oldAccount.Balance -= income.Amount;

        // Apply the new income effect
        newAccount.Balance += dto.Amount;

        income.AccountId = dto.AccountId;
        income.SourceId = dto.SourceId;
        income.Amount = dto.Amount;
        income.IncomeDate = dto.IncomeDate;
        income.Description = dto.Description;

        await _context.SaveChangesAsync();

        return Ok(income);
    }

    // DELETE: api/incomes/1
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteIncome(int id)
    {
        var income = await _context.Income
            .FirstOrDefaultAsync(i => i.IncomeId == id);

        if (income == null)
        {
            return NotFound(new
            {
                message = "Income not found."
            });
        }

        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.AccountId == income.AccountId);

        if (account == null)
        {
            return BadRequest(new
            {
                message = "Income account not found."
            });
        }

        // Reverse the income effect
        account.Balance -= income.Amount;

        _context.Income.Remove(income);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Income deleted successfully."
        });
    }


    // GET: api/incomes/summary/total
    [HttpGet("summary/total")]
    public async Task<IActionResult> GetTotalIncome()
    {
        var totalIncome = await _context.Income
            .SumAsync(i => i.Amount);

        return Ok(new
        {
            totalIncome
        });
    }

    // GET: api/incomes/summary/monthly
    [HttpGet("summary/monthly")]
    public async Task<IActionResult> GetMonthlyIncome(
        [FromQuery] int year,
        [FromQuery] int month)
    {
        var totalIncome = await _context.Income
            .Where(i =>
                i.IncomeDate.Year == year &&
                i.IncomeDate.Month == month)
            .SumAsync(i => i.Amount);

        return Ok(new
        {
            year,
            month,
            totalIncome
        });
    }

    // GET: api/incomes/summary/by-source
    [HttpGet("summary/by-source")]
    public async Task<IActionResult> GetIncomeBySource()
    {
        var incomeBySource = await _context.Income
            .Include(i => i.Source)
            .GroupBy(i => new
            {
                i.SourceId,
                SourceName = i.Source != null
                    ? i.Source.SourceName
                    : "Unknown"
            })
            .Select(g => new
            {
                sourceId = g.Key.SourceId,
                sourceName = g.Key.SourceName,
                totalIncome = g.Sum(i => i.Amount)
            })
            .OrderByDescending(x => x.totalIncome)
            .ToListAsync();

        return Ok(incomeBySource);
    }
}