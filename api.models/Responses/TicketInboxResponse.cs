namespace api.models.Responses;

// Summary view of a ticket for list/inbox displays. Includes creator and assignee names.
public class TicketInboxResponse
{
    // Ticket ID.
    public int Id { get; set; }

    // Ticket title.
    public string Title { get; set; } = string.Empty;

    // Current status: "open", "in_progress", "resolved", "closed".
    public string Status { get; set; } = string.Empty;

    // Severity: "low", "medium", "high".
    public string Priority { get; set; } = string.Empty;

    // FK of the user who created the ticket.
    public int CreatedById { get; set; }

    // Username of the creator.
    public string CreatedByUsername { get; set; } = string.Empty;

    // Display name of the creator.
    public string CreatedByFullname { get; set; } = string.Empty;

    // FK of the assigned user (null if unassigned).
    public int? AssignedToId { get; set; }

    // Username of the assigned user (null if unassigned).
    public string? AssignedToUsername { get; set; }

    // Display name of the assigned user (null if unassigned).
    public string? AssignedToFullname { get; set; }

    // Timestamp when the ticket was created.
    public DateTime CreatedAt { get; set; }

    // Timestamp of the last update (null if never updated).
    public DateTime? UpdatedAt { get; set; }
}
