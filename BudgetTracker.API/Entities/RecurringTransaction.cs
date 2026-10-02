using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BudgetTracker.API.Entities;

[Table("RecurringTransactions")]
public class RecurringTransaction
{
    [Key]
    public int RecurringTransactionId { get; set; }

    public int UserId { get; set; }

    public int AccountId { get; set; }

    public int CategoryId { get; set; }

    public int PaymentModeId { get; set; }

    [Required]
    [MaxLength(20)]
    public string TransactionType { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(20)]
    public string Frequency { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public DateTime NextOccurrenceDate { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedDate { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [ForeignKey(nameof(AccountId))]
    public Account? Account { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public Category? Category { get; set; }

    [ForeignKey(nameof(PaymentModeId))]
    public PaymentMode? PaymentMode { get; set; }
}