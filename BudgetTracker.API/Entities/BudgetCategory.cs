using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BudgetTracker.API.Entities;

[Table("BudgetCategories")]
public class BudgetCategory
{
    [Key]
    public int BudgetCategoryId { get; set; }

    public int BudgetId { get; set; }

    public int CategoryId { get; set; }

    public decimal AllocatedAmount { get; set; }

    public DateTime CreatedDate { get; set; }

    // Navigation properties
    public Budget? Budget { get; set; }

    public Category? Category { get; set; }
}