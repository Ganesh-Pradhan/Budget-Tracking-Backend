using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.API.Entities;

public class IncomeSource
{
    [Key]
    public int SourceId { get; set; }

    public string SourceName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; }
}