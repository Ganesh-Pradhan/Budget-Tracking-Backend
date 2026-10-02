using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.API.DTOs;

public class CreateAccountDto
{
    [Required]
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

    [Range(0, double.MaxValue)]
    public decimal Balance { get; set; }

    public bool IsActive { get; set; } = true;
}