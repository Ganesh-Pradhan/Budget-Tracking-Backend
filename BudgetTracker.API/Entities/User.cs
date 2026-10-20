using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BudgetTracker.API.Entities;

public class User
{
    [Key]
    public int UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    [JsonIgnore]
    public string PasswordHash { get; set; } = string.Empty;

    public string Currency { get; set; } = "INR";

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }
}