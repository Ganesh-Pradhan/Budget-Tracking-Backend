using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.API.Entities;

public class Category
{
    [Key]
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string CategoryType { get; set; } = string.Empty;

    public string? Icon { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedDate { get; set; }
}