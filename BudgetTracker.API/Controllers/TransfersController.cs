using BudgetTracker.API.Data;
using BudgetTracker.API.DTOs;
using BudgetTracker.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransfersController : ApiControllerBase
{
    private readonly BudgetTrackerDbContext _context;

    public TransfersController(BudgetTrackerDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetTransfers(
        [FromQuery] int? fromAccountId,
        [FromQuery] int? toAccountId,
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
        var query = _context.Transfers
            .Where(t => t.UserId == userId)
            .AsQueryable();

        if (fromAccountId.HasValue)
            query = query.Where(t =>
                t.FromAccountId == fromAccountId.Value);

        if (toAccountId.HasValue)
            query = query.Where(t =>
                t.ToAccountId == toAccountId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(t =>
                t.Description != null &&
                t.Description.Contains(search));
        }

        var totalRecords = await query.CountAsync();

        var totalPages =
            (int)Math.Ceiling(totalRecords / (double)pageSize);

        var transfers = await query
            .OrderByDescending(t => t.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new PaginatedResult<Transfer>
        {
            TotalRecords = totalRecords,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            Data = transfers
        };

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTransfer(int id)
    {
        var authError = RequireAuth();
        if (authError != null) return authError;

        var userId = CurrentUserId.Value;
        var transfer = await _context.Transfers
            .FirstOrDefaultAsync(t =>
                t.TransferId == id && t.UserId == userId);

        if (transfer == null)
            return NotFound(new
            {
                message = "Transfer not found."
            });

        return Ok(transfer);
    }


    [HttpPost]
    public async Task<IActionResult> CreateTransfer(CreateTransferDto dto)
    {
        var authError = RequireAuth();
        if (authError != null) return authError;

        var userId = CurrentUserId.Value;

        var fromAccount = await _context.Accounts
            .FirstOrDefaultAsync(a =>
                a.AccountId == dto.FromAccountId &&
                a.UserId == userId);

        if (fromAccount == null)
        {
            return BadRequest(new
            {
                message = "From account not found."
            });
        }

        if (!fromAccount.IsActive)
        {
            return BadRequest(new
            {
                message = "From account is inactive."
            });
        }

        var toAccount = await _context.Accounts
            .FirstOrDefaultAsync(a =>
                a.AccountId == dto.ToAccountId &&
                a.UserId == userId);

        if (toAccount == null)
        {
            return BadRequest(new
            {
                message = "To account not found."
            });
        }

        if (!toAccount.IsActive)
        {
            return BadRequest(new
            {
                message = "To account is inactive."
            });
        }

        if (dto.FromAccountId == dto.ToAccountId)
        {
            return BadRequest(new
            {
                message = "From account and to account cannot be the same."
            });
        }

        if (fromAccount.Balance < dto.Amount)
        {
            return BadRequest(new
            {
                message = "Insufficient from account balance."
            });
        }

        var transfer = new Transfer
        {
            UserId = userId,
            FromAccountId = dto.FromAccountId,
            ToAccountId = dto.ToAccountId,
            Amount = dto.Amount,
            Description = dto.Description,
            TransferDate = dto.TransferDate,
            CreatedDate = DateTime.Now
        };

        _context.Transfers.Add(transfer);

        // Move money between accounts
        fromAccount.Balance -= dto.Amount;
        toAccount.Balance += dto.Amount;

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetTransfer),
            new { id = transfer.TransferId },
            transfer
        );
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTransfer(
        int id,
        UpdateTransferDto dto)
    {
        var authError = RequireAuth();
        if (authError != null) return authError;

        var userId = CurrentUserId.Value;
        var transfer = await _context.Transfers
            .FirstOrDefaultAsync(t =>
                t.TransferId == id && t.UserId == userId);

        if (transfer == null)
        {
            return NotFound(new
            {
                message = "Transfer not found."
            });
        }

        var newFromAccount = await _context.Accounts
            .FirstOrDefaultAsync(a => a.AccountId == dto.FromAccountId);

        if (newFromAccount == null)
        {
            return BadRequest(new
            {
                message = "From account not found."
            });
        }

        if (!newFromAccount.IsActive)
        {
            return BadRequest(new
            {
                message = "From account is inactive."
            });
        }
        if (newFromAccount.UserId != transfer.UserId)
        {
            return BadRequest(new
            {
                message = "From account does not belong to the user."
            });
        }
        var newToAccount = await _context.Accounts
            .FirstOrDefaultAsync(a => a.AccountId == dto.ToAccountId);

        if (newToAccount == null)
        {
            return BadRequest(new
            {
                message = "To account not found."
            });
        }

        if (!newToAccount.IsActive)
        {
            return BadRequest(new
            {
                message = "To account is inactive."
            });
        }

        if (dto.FromAccountId == dto.ToAccountId)
        {
            return BadRequest(new
            {
                message = "From account and to account cannot be the same."
            });
        }
        if (newToAccount.UserId != transfer.UserId)
        {
            return BadRequest(new
            {
                message = "To account does not belong to the user."
            });
        }

        var oldFromAccount = await _context.Accounts
            .FirstOrDefaultAsync(a =>
                a.AccountId == transfer.FromAccountId);

        var oldToAccount = await _context.Accounts
            .FirstOrDefaultAsync(a =>
                a.AccountId == transfer.ToAccountId);

        if (oldFromAccount == null || oldToAccount == null)
        {
            return BadRequest(new
            {
                message = "Original transfer account not found."
            });
        }

        // Restore the old transfer
        oldFromAccount.Balance += transfer.Amount;
        oldToAccount.Balance -= transfer.Amount;

        // Check balance for the new transfer
        if (newFromAccount.Balance < dto.Amount)
        {
            return BadRequest(new
            {
                message = "Insufficient from account balance."
            });
        }

        // Apply the new transfer
        newFromAccount.Balance -= dto.Amount;
        newToAccount.Balance += dto.Amount;

        transfer.FromAccountId = dto.FromAccountId;
        transfer.ToAccountId = dto.ToAccountId;
        transfer.Amount = dto.Amount;
        transfer.Description = dto.Description;
        transfer.TransferDate = dto.TransferDate;

        await _context.SaveChangesAsync();

        return Ok(transfer);
    }


    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTransfer(int id)
    {
        var authError = RequireAuth();
        if (authError != null) return authError;

        var userId = CurrentUserId.Value;
        var transfer = await _context.Transfers
            .FirstOrDefaultAsync(t =>
                t.TransferId == id && t.UserId == userId);

        if (transfer == null)
        {
            return NotFound(new
            {
                message = "Transfer not found."
            });
        }

        var fromAccount = await _context.Accounts
            .FirstOrDefaultAsync(a => a.AccountId == transfer.FromAccountId);

        var toAccount = await _context.Accounts
            .FirstOrDefaultAsync(a => a.AccountId == transfer.ToAccountId);

        if (fromAccount == null || toAccount == null)
        {
            return BadRequest(new
            {
                message = "Transfer account not found."
            });
        }

        // Reverse the transfer
        fromAccount.Balance += transfer.Amount;
        toAccount.Balance -= transfer.Amount;

        _context.Transfers.Remove(transfer);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Transfer deleted successfully."
        });
    }


    [HttpGet("summary")]
    public async Task<IActionResult> GetTransferSummary()
    {
        var authError = RequireAuth();
        if (authError != null) return authError;

        var userId = CurrentUserId.Value;
        var query = _context.Transfers
            .Where(t => t.UserId == userId);

        var totalTransfers = await query.CountAsync();

        var totalAmount = await query.SumAsync(t => t.Amount);

        return Ok(new
        {
            totalTransfers,
            totalAmount
        });
    }
}