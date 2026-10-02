using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.API.DTOs;

public class UpdateExpenseDto
{
    [Required]
    public int CategoryId { get; set; }

    [Required]
    public int AccountId { get; set; }

    [Required]
    public int PaymentModeId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public DateTime ExpenseDate { get; set; }

    public string? Description { get; set; }
}