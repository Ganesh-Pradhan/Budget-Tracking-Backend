using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.API.Entities;

public class PaymentMode
{
    [Key]
    public int PaymentModeId { get; set; }

    public string ModeName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; }
}