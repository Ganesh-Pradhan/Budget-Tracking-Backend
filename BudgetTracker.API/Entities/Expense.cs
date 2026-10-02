using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BudgetTracker.API.Entities;

[Table("Expenses")]
public class Expense
{
    [Key]
    public int ExpenseId { get; set; }

    public int UserId { get; set; }

    public int CategoryId { get; set; }

    public int AccountId { get; set; }

    public int PaymentModeId { get; set; }

    public decimal Amount { get; set; }

    public DateTime ExpenseDate { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedDate { get; set; }

    // Navigation properties
    public User? User { get; set; }

    public Category? Category { get; set; }

    public PaymentMode? PaymentMode { get; set; }

    public Account? Account { get; set; }


}