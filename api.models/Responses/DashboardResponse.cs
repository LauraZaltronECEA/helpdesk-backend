namespace api.models.Responses;

// Aggregated dashboard stats returned to the client.
// Admins or users with READ_TICKET permission see global counts;
// regular users see only tickets they created.
public class DashboardResponse
{
    // -- Overview counts --

    // Total number of non-deleted tickets in the current scope.
    public int TotalTickets { get; set; }

    // Tickets with Status == "open".
    public int OpenTickets { get; set; }

    // Tickets with Status == "in_progress".
    public int InProgressTickets { get; set; }

    // Tickets with Status == "resolved".
    public int ResolvedTickets { get; set; }

    // Tickets with Status == "closed".
    public int ClosedTickets { get; set; }

    // -- Priority breakdown --

    // Tickets with Priority == "low".
    public int LowPriority { get; set; }

    // Tickets with Priority == "medium".
    public int MediumPriority { get; set; }

    // Tickets with Priority == "high".
    public int HighPriority { get; set; }

    // -- User-specific counters --

    // Tickets created by the currently logged-in user.
    public int TicketsCreatedByMe { get; set; }

    // Tickets assigned to the currently logged-in user.
    public int TicketsAssignedToMe { get; set; }

    // -- Recent activity --

    // Latest 5 tickets in the current scope, with creator info.
    public List<TicketInboxResponse> RecentTickets { get; set; } = new();
}
