using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.API.DTOs;

public class CreateTransferDto
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public int FromAccountId { get; set; }

    [Required]
    public int ToAccountId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    public DateTime TransferDate { get; set; }
}