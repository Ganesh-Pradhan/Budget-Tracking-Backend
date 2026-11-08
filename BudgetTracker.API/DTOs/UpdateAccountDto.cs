using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.API.DTOs;

public class UpdateAccountDto
{
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

    public bool IsActive { get; set; }

    /// <summary>
    /// Concurrency token received by the client when the account was last read.
    /// The server rejects the update if this does not match the current database value.
    /// </summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}