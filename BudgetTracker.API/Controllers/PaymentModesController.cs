using BudgetTracker.API.Data;
using BudgetTracker.API.DTOs;
using BudgetTracker.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentModesController : ControllerBase
{
    private readonly BudgetTrackerDbContext _context;

    public PaymentModesController(BudgetTrackerDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaymentModes(
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

        var query = _context.PaymentModes.AsQueryable();

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.ModeName.Contains(search));

        var totalRecords = await query.CountAsync();

        var totalPages = (int)Math.Ceiling(
            totalRecords / (double)pageSize);

        var paymentModes = await query
            .OrderBy(p => p.ModeName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new PaginatedResult<PaymentMode>
        {
            TotalRecords = totalRecords,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            Data = paymentModes
        };

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPaymentMode(int id)
    {
        var paymentMode = await _context.PaymentModes
            .FirstOrDefaultAsync(p => p.PaymentModeId == id);

        if (paymentMode == null)
        {
            return NotFound(new
            {
                message = "Payment mode not found."
            });
        }

        return Ok(paymentMode);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePaymentMode(
        CreatePaymentModeDto dto)
    {
        var modeName = dto.ModeName.Trim();

        if (string.IsNullOrWhiteSpace(modeName))
        {
            return BadRequest(new
            {
                message = "Payment mode name is required."
            });
        }

        var duplicateExists = await _context.PaymentModes
            .AnyAsync(p => p.ModeName == modeName);

        if (duplicateExists)
        {
            return BadRequest(new
            {
                message = "A payment mode with the same name already exists."
            });
        }

        var paymentMode = new PaymentMode
        {
            ModeName = modeName,
            Description = dto.Description,
            IsActive = dto.IsActive
        };

        _context.PaymentModes.Add(paymentMode);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetPaymentMode),
            new { id = paymentMode.PaymentModeId },
            paymentMode);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePaymentMode(
        int id,
        UpdatePaymentModeDto dto)
    {
        var paymentMode = await _context.PaymentModes
            .FirstOrDefaultAsync(p => p.PaymentModeId == id);

        if (paymentMode == null)
        {
            return NotFound(new
            {
                message = "Payment mode not found."
            });
        }

        var modeName = dto.ModeName.Trim();

        if (string.IsNullOrWhiteSpace(modeName))
        {
            return BadRequest(new
            {
                message = "Payment mode name is required."
            });
        }

        var duplicateExists = await _context.PaymentModes
            .AnyAsync(p =>
                p.PaymentModeId != id &&
                p.ModeName == modeName);

        if (duplicateExists)
        {
            return BadRequest(new
            {
                message = "A payment mode with the same name already exists."
            });
        }

        paymentMode.ModeName = modeName;
        paymentMode.Description = dto.Description;
        paymentMode.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        return Ok(paymentMode);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePaymentMode(int id)
    {
        var paymentMode = await _context.PaymentModes
            .FirstOrDefaultAsync(p => p.PaymentModeId == id);

        if (paymentMode == null)
        {
            return NotFound(new
            {
                message = "Payment mode not found."
            });
        }

        var usedInExpenses = await _context.Expenses
            .AnyAsync(e => e.PaymentModeId == id);

        if (usedInExpenses)
        {
            return BadRequest(new
            {
                message = "Payment mode cannot be deleted because it is used by an expense."
            });
        }

        var usedInTransactions = await _context.Transactions
            .AnyAsync(t => t.PaymentModeId == id);

        if (usedInTransactions)
        {
            return BadRequest(new
            {
                message = "Payment mode cannot be deleted because it is used by a transaction."
            });
        }

        var usedInRecurringTransactions = await _context.RecurringTransactions
            .AnyAsync(r => r.PaymentModeId == id);

        if (usedInRecurringTransactions)
        {
            return BadRequest(new
            {
                message = "Payment mode cannot be deleted because it is used by a recurring transaction."
            });
        }

        _context.PaymentModes.Remove(paymentMode);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Payment mode deleted successfully."
        });
    }
}