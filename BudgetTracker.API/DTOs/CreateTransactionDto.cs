using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.API.DTOs;

public class CreateTransactionDto
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Required]
    public int PaymentModeId { get; set; }

    [Required]
    public string TransactionType { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
    public decimal Amount { get; set; }

    [Required]
    public DateTime TransactionDate { get; set; }

    public string? Description { get; set; }
}