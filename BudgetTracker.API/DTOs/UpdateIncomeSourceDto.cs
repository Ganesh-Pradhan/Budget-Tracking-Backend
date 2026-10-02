using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.API.DTOs;

public class UpdateIncomeSourceDto
{
    [Required]
    [MaxLength(100)]
    public string SourceName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }

    public bool IsActive { get; set; }
}