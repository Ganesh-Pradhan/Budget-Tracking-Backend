using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.API.DTOs;

public class UpdateBudgetDto
{
    [Required]
    [Range(1, 12)]
    public int BudgetMonth { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal BudgetAmount { get; set; }
}