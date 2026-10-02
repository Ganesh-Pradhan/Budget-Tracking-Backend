using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.API.DTOs;

public class CreateBudgetDto
{
    [Required]
    public int UserId { get; set; }

    [Required]
    [Range(2000, 2100)]
    public int BudgetYear { get; set; }

    [Required]
    [Range(1, 12)]
    public int BudgetMonth { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal BudgetAmount { get; set; }
}