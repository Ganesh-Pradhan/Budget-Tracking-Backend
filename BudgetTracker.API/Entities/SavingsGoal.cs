using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BudgetTracker.API.Entities;

[Table("SavingsGoals")]
public class SavingsGoal
{
    [Key]
    public int GoalId { get; set; }

    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string GoalName { get; set; } = string.Empty;

    public decimal TargetAmount { get; set; }

    public decimal SavedAmount { get; set; }

    public DateTime? TargetDate { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    // Navigation property
    public User? User { get; set; }
}