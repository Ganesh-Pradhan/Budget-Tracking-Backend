using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.API.DTOs;

public class UpdateProfileDto
{
    [Required]
    [MinLength(2, ErrorMessage = "Name must be at least 2 characters.")]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;
}
