namespace api.models.Responses;

public class TicketDetailResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public int CreatedById { get; set; }
    public string CreatedByUsername { get; set; } = string.Empty;
    public string CreatedByFullname { get; set; } = string.Empty;
    public string CreatedByEmail { get; set; } = string.Empty;
    public int? AssignedToId { get; set; }
    public string? AssignedToUsername { get; set; }
    public string? AssignedToFullname { get; set; }
    public string? AssignedToEmail { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
