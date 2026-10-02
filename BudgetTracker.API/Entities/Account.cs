using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BudgetTracker.API.Entities;

[Table("Accounts")]
public class Account
{
    [Key]
    public int AccountId { get; set; }

    public int UserId { get; set; }

    [Required]
    [MaxLength(50)]
    public string AccountType { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string AccountName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? BankName { get; set; }

    [MaxLength(50)]
    public string? AccountNumber { get; set; }

    public decimal Balance { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedDate { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}