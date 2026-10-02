using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.API.DTOs;

public class CreateSavingsGoalDto
{
    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string GoalName { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal TargetAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal SavedAmount { get; set; }

    public DateTime? TargetDate { get; set; }
}