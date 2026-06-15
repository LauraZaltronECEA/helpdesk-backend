using System.ComponentModel.DataAnnotations;

namespace api.models.DTOs;

public class TicketUpdateDTO
{
    [StringLength(200)]
    public string? Title { get; set; }

    [StringLength(4000)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string? Status { get; set; }

    [RegularExpression("^(?i:low|medium|high)$", ErrorMessage = "Priority must be one of: low, medium, high.")]
    public string? Priority { get; set; }

    public int? AssignedToId { get; set; }
}
