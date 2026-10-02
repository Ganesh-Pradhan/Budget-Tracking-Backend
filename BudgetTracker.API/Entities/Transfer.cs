using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BudgetTracker.API.Entities;

[Table("Transfers")]
public class Transfer
{
    [Key]
    public int TransferId { get; set; }

    public int UserId { get; set; }

    public int FromAccountId { get; set; }

    public int ToAccountId { get; set; }

    public decimal Amount { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime TransferDate { get; set; }

    public DateTime CreatedDate { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [ForeignKey(nameof(FromAccountId))]
    public Account? FromAccount { get; set; }

    [ForeignKey(nameof(ToAccountId))]
    public Account? ToAccount { get; set; }
}