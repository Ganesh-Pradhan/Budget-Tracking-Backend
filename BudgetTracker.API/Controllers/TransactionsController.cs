using BudgetTracker.API.Data;
using BudgetTracker.API.DTOs;
using BudgetTracker.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ApiControllerBase
{
    private readonly BudgetTrackerDbContext _context;

    public TransactionsController(BudgetTrackerDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetTransactions(
     [FromQuery] string? search,
     [FromQuery] int? paymentModeId,
     [FromQuery] string? transactionType,
     [FromQuery] int? year,
     [FromQuery] int? month,
     [FromQuery] int page = 1,
     [FromQuery] int pageSize = 5)
    {
        var authError = RequireAuth();
        if (authError != null) return authError;

        if (page < 1)
        {
            page = 1;
        }

        if (pageSize < 1)
        {
            pageSize = 5;
        }

        if (pageSize > 100)
        {
            pageSize = 100;
        }

        var userId = CurrentUserId.Value;
        var query = _context.Transactions
            .Include(t => t.Category)
            .Include(t => t.PaymentMode)
            .Where(t => t.UserId == userId)
            .AsQueryable();

        // Search by Description
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(t =>
                t.Description != null &&
                t.Description.Contains(search));
        }

        // Filter by Payment Mode
        if (paymentModeId.HasValue)
        {
            query = query.Where(t =>
                t.PaymentModeId == paymentModeId.Value);
        }

        // Filter by Transaction Type
        if (!string.IsNullOrWhiteSpace(transactionType))
        {
            query = query.Where(t =>
                t.TransactionType == transactionType);
        }

        // Filter by Year
        if (year.HasValue)
        {
            query = query.Where(t =>
                t.TransactionDate.Year == year.Value);
        }

        // Filter by Month
        if (month.HasValue)
        {
            query = query.Where(t =>
                t.TransactionDate.Month == month.Value);
        }

        // Get total count AFTER filters
        var totalRecords = await query.CountAsync();

        // Calculate total pages
        var totalPages = (int)Math.Ceiling(
            totalRecords / (double)pageSize);

        // Get paginated records
        var transactions = await query
            .OrderByDescending(t => t.TransactionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new PaginatedResult<Transaction>
        {
            TotalRecords = totalRecords,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            Data = transactions
        };

        return Ok(result);
    }


    [HttpGet("{id}")]
    public async Task<IActionResult> GetTransactionById(int id)
    {
        var authError = RequireAuth();
        if (authError != null) return authError;

        var userId = CurrentUserId.Value;
        var transaction = await _context.Transactions
            .Include(t => t.User)
            .Include(t => t.Category)
            .Include(t => t.PaymentMode)
            .FirstOrDefaultAsync(t => t.TransactionId == id && t.UserId == userId);

        if (transaction == null)
        {
            return NotFound(new
            {
                message = $"Transaction with ID {id} was not found."
            });
        }

        return Ok(transaction);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTransaction(
      CreateTransactionDto dto)
    {
        var authError = RequireAuth();
        if (authError != null) return authError;

        var userId = CurrentUserId.Value;

        // 1. Check Payment Mode exists
        var paymentMode = await _context.PaymentModes
            .FirstOrDefaultAsync(p => p.PaymentModeId == dto.PaymentModeId);

        if (paymentMode == null)
        {
            return BadRequest(new
            {
                message = "Payment mode not found."
            });
        }

        // 2. Check Payment Mode is active
        if (!paymentMode.IsActive)
        {
            return BadRequest(new
            {
                message = "Payment mode is inactive and cannot be used."
            });
        }

        // 3. Check Category exists
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId);

        if (category == null)
        {
            return BadRequest(new
            {
                message = "Category not found."
            });
        }

        // 4. Check Category is active
        if (!category.IsActive)
        {
            return BadRequest(new
            {
                message = "Category is inactive and cannot be used."
            });
        }

        // 5. Check Category type against the requested transaction type.
        //    Expenses must use Expense categories; income transactions must
        //    use Income categories. Reject the other direction so a
        //    transaction can never mix a category from the wrong type.
        if (dto.TransactionType == "Expense" &&
            category.CategoryType != "Expense")
        {
            return BadRequest(new
            {
                message = "Expense transactions can only use Expense categories."
            });
        }

        if (dto.TransactionType == "Income" &&
            category.CategoryType != "Income")
        {
            return BadRequest(new
            {
                message = "Income transactions can only use Income categories."
            });
        }

        // 6. Check User exists
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            return BadRequest(new
            {
                message = "User not found."
            });
        }

        // 7. Check Transaction Type
        if (dto.TransactionType != "Expense" &&
            dto.TransactionType != "Need")
        {
            return BadRequest(new
            {
                message = "Invalid transaction type. Allowed values are Expense and Need."
            });
        }

        // 8. Create Transaction
        var transaction = new Transaction
        {
            UserId = userId,
            CategoryId = dto.CategoryId,
            PaymentModeId = dto.PaymentModeId,
            TransactionType = dto.TransactionType,
            Amount = dto.Amount,
            TransactionDate = dto.TransactionDate,
            Description = dto.Description,
            CreatedDate = DateTime.Now
        };

        _context.Transactions.Add(transaction);

        await _context.SaveChangesAsync();

        return Ok(transaction);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTransaction(
        int id,
        UpdateTransactionDto dto)
    {
        var authError = RequireAuth();
        if (authError != null) return authError;

        var userId = CurrentUserId.Value;
        // 1. Check transaction exists and belongs to the user
        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.TransactionId == id && t.UserId == userId);

        if (transaction == null)
        {
            return NotFound(new
            {
                message = $"Transaction with ID {id} was not found."
            });
        }

        // 2. Check Payment Mode exists
        var paymentMode = await _context.PaymentModes
            .FirstOrDefaultAsync(p => p.PaymentModeId == dto.PaymentModeId);

        if (paymentMode == null)
        {
            return BadRequest(new
            {
                message = "Payment mode not found."
            });
        }

        // 3. Check Payment Mode is active
        if (!paymentMode.IsActive)
        {
            return BadRequest(new
            {
                message = "Payment mode is inactive and cannot be used."
            });
        }

        // 4. Check Category exists
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId);

        if (category == null)
        {
            return BadRequest(new
            {
                message = "Category not found."
            });
        }

        // 5. Check Category is active
        if (!category.IsActive)
        {
            return BadRequest(new
            {
                message = "Category is inactive and cannot be used."
            });
        }

        // 6. Check Category type against the requested transaction type.
        //    Expenses must use Expense categories; income transactions must
        //    use Income categories. Reject the other direction so a
        //    transaction can never mix a category from the wrong type.
        if (dto.TransactionType == "Expense" &&
            category.CategoryType != "Expense")
        {
            return BadRequest(new
            {
                message = "Expense transactions can only use Expense categories."
            });
        }

        if (dto.TransactionType == "Income" &&
            category.CategoryType != "Income")
        {
            return BadRequest(new
            {
                message = "Income transactions can only use Income categories."
            });
        }

        // 7. Check Transaction Type
        if (dto.TransactionType != "Expense" &&
            dto.TransactionType != "Need")
        {
            return BadRequest(new
            {
                message = "Invalid transaction type. Allowed values are Expense and Need."
            });
        }

        // 8. Update transaction
        transaction.CategoryId = dto.CategoryId;
        transaction.PaymentModeId = dto.PaymentModeId;
        transaction.TransactionType = dto.TransactionType;
        transaction.Amount = dto.Amount;
        transaction.TransactionDate = dto.TransactionDate;
        transaction.Description = dto.Description;

        await _context.SaveChangesAsync();

        return Ok(transaction);
    }


    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTransaction(int id)
    {
        var authError = RequireAuth();
        if (authError != null) return authError;

        var userId = CurrentUserId.Value;
        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.TransactionId == id && t.UserId == userId);

        if (transaction == null)
        {
            return NotFound(new
            {
                message = $"Transaction with ID {id} was not found."
            });
        }

        _context.Transactions.Remove(transaction);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = $"Transaction with ID {id} deleted successfully."
        });
    }

}

