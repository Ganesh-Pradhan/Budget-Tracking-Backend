using BudgetTracker.API.Data;
using BudgetTracker.API.DTOs;
using BudgetTracker.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SavingsGoalsController : ControllerBase
{
    private readonly BudgetTrackerDbContext _context;

    public SavingsGoalsController(BudgetTrackerDbContext context)
    {
        _context = context;
    }

    // GET: api/SavingsGoals
    [HttpGet]
    public async Task<IActionResult> GetSavingsGoals(
        [FromQuery] int? userId,
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

        var query = _context.SavingsGoals
            .AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(g =>
                g.UserId == userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(g =>
                g.GoalName.Contains(search));
        }

        var totalRecords = await query.CountAsync();

        var totalPages = (int)Math.Ceiling(
            totalRecords / (double)pageSize);

        var goals = await query
            .OrderByDescending(g => g.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new PaginatedResult<SavingsGoal>
        {
            TotalRecords = totalRecords,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            Data = goals
        };

        return Ok(result);
    }

    // GET: api/SavingsGoals/1
    [HttpGet("{id}")]
    public async Task<IActionResult> GetSavingsGoal(int id)
    {
        var goal = await _context.SavingsGoals
            .FirstOrDefaultAsync(g =>
                g.GoalId == id);

        if (goal == null)
        {
            return NotFound(new
            {
                message = "Savings goal not found."
            });
        }

        return Ok(goal);
    }

    // POST: api/SavingsGoals
    [HttpPost]
    public async Task<IActionResult> CreateSavingsGoal(
        CreateSavingsGoalDto dto)
    {
        var userExists = await _context.Users
            .AnyAsync(u => u.UserId == dto.UserId);

        if (!userExists)
        {
            return BadRequest(new
            {
                message = "User not found."
            });
        }

        if (dto.SavedAmount > dto.TargetAmount)
        {
            return BadRequest(new
            {
                message = "Saved amount cannot be greater than target amount."
            });
        }

        var goal = new SavingsGoal
        {
            UserId = dto.UserId,
            GoalName = dto.GoalName,
            TargetAmount = dto.TargetAmount,
            SavedAmount = dto.SavedAmount,
            TargetDate = dto.TargetDate,
            CreatedDate = DateTime.Now
        };

        _context.SavingsGoals.Add(goal);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetSavingsGoal),
            new { id = goal.GoalId },
            goal
        );
    }

    // PUT: api/SavingsGoals/1
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSavingsGoal(
        int id,
        UpdateSavingsGoalDto dto)
    {
        var goal = await _context.SavingsGoals
            .FirstOrDefaultAsync(g =>
                g.GoalId == id);

        if (goal == null)
        {
            return NotFound(new
            {
                message = "Savings goal not found."
            });
        }

        if (dto.SavedAmount > dto.TargetAmount)
        {
            return BadRequest(new
            {
                message = "Saved amount cannot be greater than target amount."
            });
        }

        goal.GoalName = dto.GoalName;
        goal.TargetAmount = dto.TargetAmount;
        goal.SavedAmount = dto.SavedAmount;
        goal.TargetDate = dto.TargetDate;
        goal.UpdatedDate = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(goal);
    }

    // DELETE: api/SavingsGoals/1
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSavingsGoal(int id)
    {
        var goal = await _context.SavingsGoals
            .FirstOrDefaultAsync(g =>
                g.GoalId == id);

        if (goal == null)
        {
            return NotFound(new
            {
                message = "Savings goal not found."
            });
        }

        _context.SavingsGoals.Remove(goal);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Savings goal deleted successfully."
        });
    }


    [HttpGet("summary/total-target")]
    public async Task<IActionResult> GetTotalTargetAmount()
    {
        var totalTarget = await _context.SavingsGoals
            .SumAsync(g => g.TargetAmount);

        return Ok(new
        {
            totalTarget
        });
    }


    [HttpGet("summary/total-saved")]
    public async Task<IActionResult> GetTotalSavedAmount()
    {
        var totalSaved = await _context.SavingsGoals
            .SumAsync(g => g.SavedAmount);

        return Ok(new
        {
            totalSaved
        });
    }


    [HttpGet("summary/progress")]
    public async Task<IActionResult> GetSavingsGoalProgress()
    {
        var goals = await _context.SavingsGoals
            .Select(g => new
            {
                goalId = g.GoalId,
                goalName = g.GoalName,
                targetAmount = g.TargetAmount,
                savedAmount = g.SavedAmount,
                remainingAmount = g.TargetAmount - g.SavedAmount,
                progressPercentage = g.TargetAmount > 0
                    ? (g.SavedAmount / g.TargetAmount) * 100
                    : 0
            })
            .OrderByDescending(g => g.progressPercentage)
            .ToListAsync();

        return Ok(goals);
    }
}