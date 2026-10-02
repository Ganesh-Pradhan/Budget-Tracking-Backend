using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.API.DTOs;

public class UpdateBudgetCategoryDto
{
    [Required]
    public int CategoryId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal AllocatedAmount { get; set; }
}