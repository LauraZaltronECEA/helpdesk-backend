namespace api.models.Responses;

// Response returned after creating a ticket, including the new ticket's ID.
public class TicketCreateResponse : GeneralResponse
{
    // ID of the newly created ticket.
    public int TicketId { get; set; }
}
