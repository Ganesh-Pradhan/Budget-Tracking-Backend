using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.API.DTOs;

public class CreatePaymentModeDto
{
    [Required]
    [MaxLength(50)]
    public string ModeName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}