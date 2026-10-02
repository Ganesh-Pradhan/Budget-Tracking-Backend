using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.API.DTOs;

public class CreateIncomeDto
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public int AccountId { get; set; }

    [Required]
    public int SourceId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public DateTime IncomeDate { get; set; }

    public string? Description { get; set; }
}