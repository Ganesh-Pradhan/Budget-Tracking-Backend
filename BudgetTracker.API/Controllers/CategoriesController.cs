using BudgetTracker.API.Data;
using BudgetTracker.API.DTOs;
using BudgetTracker.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly BudgetTrackerDbContext _context;

    public CategoriesController(BudgetTrackerDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetCategories(
        [FromQuery] string? categoryType,
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

        var query = _context.Categories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(categoryType))
            query = query.Where(c => c.CategoryType == categoryType);

        if (isActive.HasValue)
            query = query.Where(c => c.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.CategoryName.Contains(search));

        var totalRecords = await query.CountAsync();

        var totalPages = (int)Math.Ceiling(
            totalRecords / (double)pageSize);

        var categories = await query
            .OrderByDescending(c => c.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new PaginatedResult<Category>
        {
            TotalRecords = totalRecords,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            Data = categories
        };

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCategory(int id)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category == null)
        {
            return NotFound(new
            {
                message = "Category not found."
            });
        }

        return Ok(category);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory(CreateCategoryDto dto)
    {
        var categoryType = dto.CategoryType.Trim();

        if (categoryType != "Income" && categoryType != "Expense")
        {
            return BadRequest(new
            {
                message = "Category type must be Income or Expense."
            });
        }

        var categoryName = dto.CategoryName.Trim();

        var duplicateExists = await _context.Categories.AnyAsync(c =>
            c.CategoryName == categoryName &&
            c.CategoryType == categoryType);

        if (duplicateExists)
        {
            return BadRequest(new
            {
                message = "A category with the same name and type already exists."
            });
        }

        var category = new Category
        {
            CategoryName = categoryName,
            CategoryType = categoryType,
            Icon = dto.Icon,
            IsActive = dto.IsActive,
            CreatedDate = DateTime.Now
        };

        _context.Categories.Add(category);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetCategory),
            new { id = category.CategoryId },
            category);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(
        int id,
        UpdateCategoryDto dto)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category == null)
        {
            return NotFound(new
            {
                message = "Category not found."
            });
        }

        var categoryType = dto.CategoryType.Trim();

        if (categoryType != "Income" && categoryType != "Expense")
        {
            return BadRequest(new
            {
                message = "Category type must be Income or Expense."
            });
        }

        var categoryName = dto.CategoryName.Trim();

        var duplicateExists = await _context.Categories.AnyAsync(c =>
            c.CategoryId != id &&
            c.CategoryName == categoryName &&
            c.CategoryType == categoryType);

        if (duplicateExists)
        {
            return BadRequest(new
            {
                message = "A category with the same name and type already exists."
            });
        }

        category.CategoryName = categoryName;
        category.CategoryType = categoryType;
        category.Icon = dto.Icon;
        category.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        return Ok(category);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category == null)
        {
            return NotFound(new
            {
                message = "Category not found."
            });
        }

        var usedInExpenses = await _context.Expenses
            .AnyAsync(e => e.CategoryId == id);

        if (usedInExpenses)
        {
            return BadRequest(new
            {
                message = "Category cannot be deleted because it is used by an expense."
            });
        }

        var usedInTransactions = await _context.Transactions
            .AnyAsync(t => t.CategoryId == id);

        if (usedInTransactions)
        {
            return BadRequest(new
            {
                message = "Category cannot be deleted because it is used by a transaction."
            });
        }

        var usedInBudgetCategories = await _context.BudgetCategories
            .AnyAsync(bc => bc.CategoryId == id);

        if (usedInBudgetCategories)
        {
            return BadRequest(new
            {
                message = "Category cannot be deleted because it is assigned to a budget."
            });
        }

        _context.Categories.Remove(category);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Category deleted successfully."
        });
    }
}