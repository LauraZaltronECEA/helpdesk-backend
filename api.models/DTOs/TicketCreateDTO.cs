using System.ComponentModel.DataAnnotations;

namespace api.models.DTOs;

public class TicketCreateDTO
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    [RegularExpression("^(?i:low|medium|high)$", ErrorMessage = "Priority must be one of: low, medium, high.")]
    public string? Priority { get; set; } = "medium";
}
