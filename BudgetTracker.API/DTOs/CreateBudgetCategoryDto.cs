using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.API.DTOs;

public class CreateBudgetCategoryDto
{
    [Required]
    public int BudgetId { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal AllocatedAmount { get; set; }
}