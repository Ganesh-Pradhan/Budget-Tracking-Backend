using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BudgetTracker.API.Entities;

[Table("Transactions")]
public class Transaction
{
    [Key]
    public int TransactionId { get; set; }

    public int UserId { get; set; }

    public int CategoryId { get; set; }

    public int PaymentModeId { get; set; }

    public decimal Amount { get; set; }

    public string TransactionType { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime TransactionDate { get; set; }

    // Add this property
    public DateTime CreatedDate { get; set; }

    // Navigation properties
    public User? User { get; set; }

    public Category? Category { get; set; }

    public PaymentMode? PaymentMode { get; set; }
}