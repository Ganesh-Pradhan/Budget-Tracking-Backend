using BudgetTracker.API.Data;
using BudgetTracker.API.DTOs;
using BudgetTracker.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IncomeSourcesController : ApiControllerBase
{
    private readonly BudgetTrackerDbContext _context;

    public IncomeSourcesController(BudgetTrackerDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetIncomeSources(
        [FromQuery] bool? isActive,
        [FromQuery] string? search,
        int page = 1,
        int pageSize = 5)
    {
        if (page < 1)
            page = 1;

        if (pageSize < 1)
            pageSize = 5;

        if (pageSize > 100)
            pageSize = 100;

        var query = _context.IncomeSources.AsQueryable();

        if (isActive.HasValue)
            query = query.Where(s => s.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s => s.SourceName.Contains(search));

        var totalRecords = await query.CountAsync();

        var totalPages = (int)Math.Ceiling(
            totalRecords / (double)pageSize);

        var incomeSources = await query
            .OrderBy(s => s.SourceName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new PaginatedResult<IncomeSource>
        {
            TotalRecords = totalRecords,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            Data = incomeSources
        };

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetIncomeSource(int id)
    {
        var incomeSource = await _context.IncomeSources
            .FirstOrDefaultAsync(s => s.SourceId == id);

        if (incomeSource == null)
        {
            return NotFound(new
            {
                message = "Income source not found."
            });
        }

        return Ok(incomeSource);
    }

    [HttpPost]
    public async Task<IActionResult> CreateIncomeSource(
        CreateIncomeSourceDto dto)
    {
        var sourceName = dto.SourceName.Trim();

        if (string.IsNullOrWhiteSpace(sourceName))
        {
            return BadRequest(new
            {
                message = "Income source name is required."
            });
        }

        var duplicateExists = await _context.IncomeSources
            .AnyAsync(s => s.SourceName == sourceName);

        if (duplicateExists)
        {
            return BadRequest(new
            {
                message = "An income source with the same name already exists."
            });
        }

        var incomeSource = new IncomeSource
        {
            SourceName = sourceName,
            Description = dto.Description,
            IsActive = dto.IsActive
        };

        _context.IncomeSources.Add(incomeSource);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetIncomeSource),
            new { id = incomeSource.SourceId },
            incomeSource);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateIncomeSource(
        int id,
        UpdateIncomeSourceDto dto)
    {
        var incomeSource = await _context.IncomeSources
            .FirstOrDefaultAsync(s => s.SourceId == id);

        if (incomeSource == null)
        {
            return NotFound(new
            {
                message = "Income source not found."
            });
        }

        var sourceName = dto.SourceName.Trim();

        if (string.IsNullOrWhiteSpace(sourceName))
        {
            return BadRequest(new
            {
                message = "Income source name is required."
            });
        }

        var duplicateExists = await _context.IncomeSources
            .AnyAsync(s =>
                s.SourceId != id &&
                s.SourceName == sourceName);

        if (duplicateExists)
        {
            return BadRequest(new
            {
                message = "An income source with the same name already exists."
            });
        }

        incomeSource.SourceName = sourceName;
        incomeSource.Description = dto.Description;
        incomeSource.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        return Ok(incomeSource);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteIncomeSource(int id)
    {
        var incomeSource = await _context.IncomeSources
            .FirstOrDefaultAsync(s => s.SourceId == id);

        if (incomeSource == null)
        {
            return NotFound(new
            {
                message = "Income source not found."
            });
        }

        var usedInIncome = await _context.Income
            .AnyAsync(i => i.SourceId == id);

        if (usedInIncome)
        {
            return BadRequest(new
            {
                message = "Income source cannot be deleted because it is used by an income record."
            });
        }

        _context.IncomeSources.Remove(incomeSource);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Income source deleted successfully."
        });
    }
}