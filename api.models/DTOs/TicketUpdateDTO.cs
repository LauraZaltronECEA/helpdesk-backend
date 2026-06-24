using System.ComponentModel.DataAnnotations;

namespace api.models.DTOs;

// Data for updating an existing ticket. All fields are optional — only provided fields are applied.
public class TicketUpdateDTO
{
    // Updated title (max 200 chars).
    [StringLength(200)]
    public string? Title { get; set; }

    //// Updated description (max 4000 chars).
    //[StringLength(4000)]
    //public string? Description { get; set; }

    // Updated status: "open", "in_progress", "resolved", or "closed".
    [StringLength(50)]
    public string? Status { get; set; }

    // Updated priority: "low", "medium", or "high".
    [RegularExpression("^(?i:low|medium|high)$", ErrorMessage = "Priority must be one of: low, medium, high.")]
    public string? Priority { get; set; }

    // User ID to assign the ticket to (null to unassign).
    public int? AssignedToId { get; set; }
}
