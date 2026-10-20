using BudgetTracker.API.Data;
using BudgetTracker.API.DTOs;
using BudgetTracker.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BudgetCategoriesController : ApiControllerBase
{
    private readonly BudgetTrackerDbContext _context;

    public BudgetCategoriesController(BudgetTrackerDbContext context)
    {
        _context = context;
    }

    // GET: api/BudgetCategories
    [HttpGet]
    public async Task<IActionResult> GetBudgetCategories(
        [FromQuery] int? budgetId,
        [FromQuery] int? categoryId,
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
        var query = _context.BudgetCategories
            .Include(bc => bc.Category)
            .Include(bc => bc.Budget)
            .Where(bc => bc.Budget!.UserId == userId)
            .AsQueryable();

        if (budgetId.HasValue)
        {
            query = query.Where(bc =>
                bc.BudgetId == budgetId.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(bc =>
                bc.CategoryId == categoryId.Value);
        }

        var totalRecords = await query.CountAsync();

        var totalPages = (int)Math.Ceiling(
            totalRecords / (double)pageSize);

        var budgetCategories = await query
            .OrderByDescending(bc => bc.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new PaginatedResult<BudgetCategory>
        {
            TotalRecords = totalRecords,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            Data = budgetCategories
        };

        return Ok(result);
    }

    // GET: api/BudgetCategories/1
    [HttpGet("{id}")]
    public async Task<IActionResult> GetBudgetCategory(int id)
    {
        var authError = RequireAuth();
        if (authError != null) return authError;

        var userId = CurrentUserId.Value;
        var budgetCategory = await _context.BudgetCategories
            .Include(bc => bc.Category)
            .Include(bc => bc.Budget)
            .FirstOrDefaultAsync(bc =>
                bc.BudgetCategoryId == id &&
                bc.Budget!.UserId == userId);

        if (budgetCategory == null)
        {
            return NotFound(new
            {
                message = "Budget category not found."
            });
        }

        return Ok(budgetCategory);
    }

    // POST: api/BudgetCategories
    [HttpPost]
    public async Task<IActionResult> CreateBudgetCategory(
        CreateBudgetCategoryDto dto)
    {
        var authError = RequireAuth();
        if (authError != null) return authError;

        var userId = CurrentUserId.Value;
        var budgetExists = await _context.Budgets
            .AnyAsync(b =>
                b.BudgetId == dto.BudgetId &&
                b.UserId == userId);

        if (!budgetExists)
        {
            return BadRequest(new
            {
                message = "Budget not found."
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

        var duplicateExists = await _context.BudgetCategories
            .AnyAsync(bc =>
                bc.BudgetId == dto.BudgetId &&
                bc.CategoryId == dto.CategoryId);

        if (duplicateExists)
        {
            return BadRequest(new
            {
                message = "This category is already assigned to the budget."
            });
        }

        var budgetCategory = new BudgetCategory
        {
            BudgetId = dto.BudgetId,
            CategoryId = dto.CategoryId,
            AllocatedAmount = dto.AllocatedAmount,
            CreatedDate = DateTime.Now
        };

        _context.BudgetCategories.Add(budgetCategory);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetBudgetCategory),
            new { id = budgetCategory.BudgetCategoryId },
            budgetCategory
        );
    }

    // PUT: api/BudgetCategories/1
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBudgetCategory(
        int id,
        UpdateBudgetCategoryDto dto)
    {
        var authError = RequireAuth();
        if (authError != null) return authError;

        var userId = CurrentUserId.Value;
        var budgetCategory = await _context.BudgetCategories
            .FirstOrDefaultAsync(bc =>
                bc.BudgetCategoryId == id &&
                bc.Budget!.UserId == userId);

        if (budgetCategory == null)
        {
            return NotFound(new
            {
                message = "Budget category not found."
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

        var duplicateExists = await _context.BudgetCategories
            .AnyAsync(bc =>
                bc.BudgetId == budgetCategory.BudgetId &&
                bc.CategoryId == dto.CategoryId &&
                bc.BudgetCategoryId != id);

        if (duplicateExists)
        {
            return BadRequest(new
            {
                message = "This category is already assigned to the budget."
            });
        }

        budgetCategory.CategoryId = dto.CategoryId;
        budgetCategory.AllocatedAmount = dto.AllocatedAmount;

        await _context.SaveChangesAsync();

        return Ok(budgetCategory);
    }

    // DELETE: api/BudgetCategories/1
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBudgetCategory(int id)
    {
        var authError = RequireAuth();
        if (authError != null) return authError;

        var userId = CurrentUserId.Value;
        var budgetCategory = await _context.BudgetCategories
            .FirstOrDefaultAsync(bc =>
                bc.BudgetCategoryId == id &&
                bc.Budget!.UserId == userId);

        if (budgetCategory == null)
        {
            return NotFound(new
            {
                message = "Budget category not found."
            });
        }

        _context.BudgetCategories.Remove(budgetCategory);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Budget category deleted successfully."
        });
    }


    [HttpGet("summary/total")]
    public async Task<IActionResult> GetTotalAllocatedAmount()
    {
        var authError = RequireAuth();
        if (authError != null) return authError;

        var userId = CurrentUserId.Value;
        var totalAllocated = await _context.BudgetCategories
            .Where(bc => bc.Budget!.UserId == userId)
            .SumAsync(bc => bc.AllocatedAmount);

        return Ok(new
        {
            totalAllocated
        });
    }

    [HttpGet("summary/by-category")]
    public async Task<IActionResult> GetAllocationByCategory()
    {
        var authError = RequireAuth();
        if (authError != null) return authError;

        var userId = CurrentUserId.Value;
        var allocationByCategory = await _context.BudgetCategories
            .Include(bc => bc.Category)
            .Where(bc => bc.Budget!.UserId == userId)
            .GroupBy(bc => new
            {
                bc.CategoryId,
                CategoryName = bc.Category != null
                    ? bc.Category.CategoryName
                    : "Unknown"
            })
            .Select(g => new
            {
                categoryId = g.Key.CategoryId,
                categoryName = g.Key.CategoryName,
                totalAllocated = g.Sum(bc => bc.AllocatedAmount)
            })
            .OrderByDescending(x => x.totalAllocated)
            .ToListAsync();

        return Ok(allocationByCategory);
    }

    [HttpGet("summary/vs-budget")]
    public async Task<IActionResult> GetBudgetVsAllocated(
    [FromQuery] int budgetId)
    {
        var authError = RequireAuth();
        if (authError != null) return authError;

        var userId = CurrentUserId.Value;
        var budget = await _context.Budgets
            .FirstOrDefaultAsync(b =>
                b.BudgetId == budgetId &&
                b.UserId == userId);

        if (budget == null)
        {
            return NotFound(new
            {
                message = "Budget not found."
            });
        }

        var totalAllocated = await _context.BudgetCategories
            .Where(bc => bc.BudgetId == budgetId)
            .SumAsync(bc => bc.AllocatedAmount);

        var remainingAmount = budget.BudgetAmount - totalAllocated;

        return Ok(new
        {
            budgetId,
            budgetAmount = budget.BudgetAmount,
            totalAllocated,
            remainingAmount
        });
    }
}