using BudgetTracker.API.Data;
using BudgetTracker.API.DTOs;
using BudgetTracker.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecurringTransactionsController : ApiControllerBase
{
    private readonly BudgetTrackerDbContext _context;

    public RecurringTransactionsController(BudgetTrackerDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecurringTransactions(
        [FromQuery] int? accountId,
        [FromQuery] string? transactionType,
        [FromQuery] string? frequency,
        [FromQuery] bool? isActive,
        [FromQuery] string? search,
        int page = 1,
        int pageSize = 5)
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
        var query = _context.RecurringTransactions
            .Where(r => r.UserId == userId)
            .AsQueryable();

        if (accountId.HasValue)
            query = query.Where(r => r.AccountId == accountId.Value);

        if (!string.IsNullOrWhiteSpace(transactionType))
            query = query.Where(r =>
                r.TransactionType == transactionType);

        if (!string.IsNullOrWhiteSpace(frequency))
            query = query.Where(r =>
                r.Frequency == frequency);

        if (isActive.HasValue)
            query = query.Where(r =>
                r.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r =>
                r.Description != null &&
                r.Description.Contains(search));

        var totalRecords = await query.CountAsync();

        var totalPages = (int)Math.Ceiling(
            totalRecords / (double)pageSize
        );

        var recurringTransactions = await query
            .OrderByDescending(r => r.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new PaginatedResult<RecurringTransaction>
        {
            TotalRecords = totalRecords,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            Data = recurringTransactions
        };

        return Ok(result);
    }


    [HttpGet("{id}")]
    public async Task<IActionResult> GetRecurringTransaction(int id)
    {
        var authError = RequireAuth();
        if (authError != null) return authError;

        var userId = CurrentUserId.Value;
        var recurringTransaction = await _context.RecurringTransactions
            .FirstOrDefaultAsync(r =>
                r.RecurringTransactionId == id && r.UserId == userId);

        if (recurringTransaction == null)
            return NotFound(new
            {
                message = "Recurring transaction not found."
            });

        return Ok(recurringTransaction);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRecurringTransaction(
    CreateRecurringTransactionDto dto)
    {
        var authError = RequireAuth();
        if (authError != null) return authError;

        var userId = CurrentUserId.Value;

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

        var categoryExists = await _context.Categories
            .AnyAsync(c => c.CategoryId == dto.CategoryId);

        if (!categoryExists)
            return BadRequest(new
            {
                message = "Category not found."
            });

        var paymentModeExists = await _context.PaymentModes
            .AnyAsync(p => p.PaymentModeId == dto.PaymentModeId);

        if (!paymentModeExists)
            return BadRequest(new
            {
                message = "Payment mode not found."
            });

        if (dto.EndDate.HasValue &&
            dto.EndDate.Value.Date < dto.StartDate.Date)
            return BadRequest(new
            {
                message = "End date cannot be before start date."
            });

        if (dto.NextOccurrenceDate.Date < dto.StartDate.Date)
            return BadRequest(new
            {
                message = "Next occurrence date cannot be before start date."
            });

        var recurringTransaction = new RecurringTransaction
        {
            UserId = userId,
            AccountId = dto.AccountId,
            CategoryId = dto.CategoryId,
            PaymentModeId = dto.PaymentModeId,
            TransactionType = dto.TransactionType,
            Amount = dto.Amount,
            Description = dto.Description,
            Frequency = dto.Frequency,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            NextOccurrenceDate = dto.NextOccurrenceDate,
            IsActive = dto.IsActive,
            CreatedDate = DateTime.Now
        };

        _context.RecurringTransactions.Add(recurringTransaction);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetRecurringTransaction),
            new { id = recurringTransaction.RecurringTransactionId },
            recurringTransaction
        );
    }


    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRecurringTransaction(
    int id,
    UpdateRecurringTransactionDto dto)
    {
        var authError = RequireAuth();
        if (authError != null) return authError;

        var userId = CurrentUserId.Value;
        var recurringTransaction = await _context.RecurringTransactions
            .FirstOrDefaultAsync(r =>
                r.RecurringTransactionId == id && r.UserId == userId);

        if (recurringTransaction == null)
            return NotFound(new
            {
                message = "Recurring transaction not found."
            });

        if (dto.EndDate.HasValue &&
            dto.EndDate.Value.Date < dto.StartDate.Date)
            return BadRequest(new
            {
                message = "End date cannot be before start date."
            });

        if (dto.NextOccurrenceDate.Date < dto.StartDate.Date)
            return BadRequest(new
            {
                message = "Next occurrence date cannot be before start date."
            });

        recurringTransaction.TransactionType = dto.TransactionType;
        recurringTransaction.Amount = dto.Amount;
        recurringTransaction.Description = dto.Description;
        recurringTransaction.Frequency = dto.Frequency;
        recurringTransaction.StartDate = dto.StartDate;
        recurringTransaction.EndDate = dto.EndDate;
        recurringTransaction.NextOccurrenceDate =
            dto.NextOccurrenceDate;
        recurringTransaction.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        return Ok(recurringTransaction);
    }


    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRecurringTransaction(int id)
    {
        var authError = RequireAuth();
        if (authError != null) return authError;

        var userId = CurrentUserId.Value;
        var recurringTransaction = await _context.RecurringTransactions
            .FirstOrDefaultAsync(r =>
                r.RecurringTransactionId == id && r.UserId == userId);

        if (recurringTransaction == null)
            return NotFound(new
            {
                message = "Recurring transaction not found."
            });

        _context.RecurringTransactions.Remove(recurringTransaction);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Recurring transaction deleted successfully."
        });
    }


    [HttpGet("summary")]
    public async Task<IActionResult> GetRecurringTransactionSummary()
    {
        var authError = RequireAuth();
        if (authError != null) return authError;

        var userId = CurrentUserId.Value;
        var query = _context.RecurringTransactions
            .Where(r => r.UserId == userId);

        var totalRecurringTransactions = await query.CountAsync();

        var totalAmount = await query.SumAsync(r => r.Amount);

        var activeRecurringTransactions =
            await query.CountAsync(r => r.IsActive);

        var inactiveRecurringTransactions =
            await query.CountAsync(r => !r.IsActive);

        return Ok(new
        {
            totalRecurringTransactions,
            totalAmount,
            activeRecurringTransactions,
            inactiveRecurringTransactions
        });
    }
}