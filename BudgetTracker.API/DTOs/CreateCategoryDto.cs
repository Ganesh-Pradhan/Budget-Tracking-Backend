using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.API.DTOs;

public class CreateCategoryDto
{
    [Required]
    [MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string CategoryType { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Icon { get; set; }

    public bool IsActive { get; set; } = true;
}