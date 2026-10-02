using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.API.DTOs;

public class CreateRecurringTransactionDto
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public int AccountId { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Required]
    public int PaymentModeId { get; set; }

    [Required]
    [MaxLength(20)]
    public string TransactionType { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(20)]
    public string Frequency { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [Required]
    public DateTime NextOccurrenceDate { get; set; }

    public bool IsActive { get; set; } = true;
}