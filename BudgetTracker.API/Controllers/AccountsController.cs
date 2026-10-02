using BudgetTracker.API.Data;
using BudgetTracker.API.DTOs;
using BudgetTracker.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly BudgetTrackerDbContext _context;

    public AccountsController(BudgetTrackerDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAccounts(
        [FromQuery] int? userId,
        [FromQuery] string? accountType,
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        int page = 1,
        int pageSize = 5)
    {
        if (page < 1)
            page = 1;

        if (pageSize < 1)
            pageSize = 5;

        if (pageSize > 100)
            pageSize = 100;

        var query = _context.Accounts.AsQueryable();

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        if (!string.IsNullOrWhiteSpace(accountType))
            query = query.Where(a => a.AccountType == accountType);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => a.AccountName.Contains(search));

        if (isActive.HasValue)
            query = query.Where(a => a.IsActive == isActive.Value);

        var totalRecords = await query.CountAsync();

        var totalPages = (int)Math.Ceiling(
            totalRecords / (double)pageSize
        );

        var accounts = await query
            .OrderByDescending(a => a.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new PaginatedResult<Account>
        {
            TotalRecords = totalRecords,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            Data = accounts
        };

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAccount(int id)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.AccountId == id);

        if (account == null)
            return NotFound(new
            {
                message = "Account not found."
            });

        return Ok(account);
    }


    [HttpPost]
    public async Task<IActionResult> CreateAccount(CreateAccountDto dto)
    {
        var userExists = await _context.Users
            .AnyAsync(u => u.UserId == dto.UserId);

        if (!userExists)
            return BadRequest(new
            {
                message = "User not found."
            });

        var account = new Account
        {
            UserId = dto.UserId,
            AccountType = dto.AccountType,
            AccountName = dto.AccountName,
            BankName = dto.BankName,
            AccountNumber = dto.AccountNumber,
            Balance = dto.Balance,
            IsActive = dto.IsActive,
            CreatedDate = DateTime.Now
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetAccount),
            new { id = account.AccountId },
            account
        );
    }


    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAccount(
    int id,
    UpdateAccountDto dto)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.AccountId == id);

        if (account == null)
            return NotFound(new
            {
                message = "Account not found."
            });

        account.AccountType = dto.AccountType;
        account.AccountName = dto.AccountName;
        account.BankName = dto.BankName;
        account.AccountNumber = dto.AccountNumber;
        account.Balance = dto.Balance;
        account.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        return Ok(account);
    }


    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAccount(int id)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.AccountId == id);

        if (account == null)
            return NotFound(new
            {
                message = "Account not found."
            });
        if (account.Balance != 0)
        {
            return BadRequest(new
            {
                message = "Account cannot be deleted while it has a balance."
            });
        }
        _context.Accounts.Remove(account);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Account deleted successfully."
        });
    }



    [HttpGet("summary")]
    public async Task<IActionResult> GetAccountSummary(
    [FromQuery] int? userId)
    {
        var query = _context.Accounts.AsQueryable();

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        var totalAccounts = await query.CountAsync();

        var totalBalance = await query
            .SumAsync(a => a.Balance);

        var activeAccounts = await query
            .CountAsync(a => a.IsActive);

        var inactiveAccounts = await query
            .CountAsync(a => !a.IsActive);

        return Ok(new
        {
            totalAccounts,
            totalBalance,
            activeAccounts,
            inactiveAccounts
        });
    }
}