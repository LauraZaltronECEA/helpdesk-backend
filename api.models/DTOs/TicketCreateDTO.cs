using System.ComponentModel.DataAnnotations;

namespace api.models.DTOs;

// Data required to create a new ticket.
public class TicketCreateDTO
{
    // Short summary of the issue (required, max 200 chars).
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    // Detailed description of the issue (required, max 4000 chars).
    [Required]
    [StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    // Severity level: "low", "medium" (default), or "high".
    [RegularExpression("^(?i:low|medium|high)$", ErrorMessage = "Priority must be one of: low, medium, high.")]
    public string? Priority { get; set; } = "medium";
}
