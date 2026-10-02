using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BudgetTracker.API.Entities;

[Table("Budgets")]
public class Budget
{
    [Key]
    public int BudgetId { get; set; }

    public int UserId { get; set; }

    public int BudgetYear { get; set; }

    public int BudgetMonth { get; set; }

    public decimal BudgetAmount { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public User? User { get; set; }
}