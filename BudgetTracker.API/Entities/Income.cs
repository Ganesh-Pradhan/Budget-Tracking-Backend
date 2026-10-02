using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BudgetTracker.API.Entities;

[Table("Income")]
public class Income
{
    [Key]
    public int IncomeId { get; set; }

    public int UserId { get; set; }

    public int SourceId { get; set; }

    public decimal Amount { get; set; }

    public int AccountId { get; set; }

    public DateTime IncomeDate { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedDate { get; set; }

    // Navigation properties
    public User? User { get; set; }

    public IncomeSource? Source { get; set; }


    public Account? Account { get; set; }
   
}