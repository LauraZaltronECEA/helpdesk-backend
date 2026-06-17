using api.models.DTOs;
using api.models.Entities;
using api.models.Responses;

namespace api.services.Repositories;

// Defines CRUD operations for tickets with role-based and permission-based access control.
public interface ITicketRepository
{
    // Returns all non-deleted tickets (admins and users with READ_TICKET see all; others see only their own).
    Task<IEnumerable<Ticket>> GetAll(int currentUserId, string role);

    // Returns tickets relevant to the user's inbox, scoped by role/permissions.
    Task<IEnumerable<TicketInboxResponse>> GetInbox(int currentUserId, string role);

    // Gets a single ticket by ID. Returns null if not found or not accessible.
    Task<Ticket?> GetById(int id, int currentUserId, string role);

    // Gets a single ticket with full detail (description + contact info).
    Task<TicketDetailResponse?> GetDetail(int id, int currentUserId, string role);

    // Creates a new ticket for the given user (requires CREATE_TICKET permission).
    Task<TicketCreateResponse> Create(TicketCreateDTO dto, int createdById);

    // Updates an existing ticket (requires UPDATE_TICKET permission or admin role).
    Task<GeneralResponse> Update(int id, TicketUpdateDTO dto, int currentUserId, string role);

    // Soft-deletes a ticket (sets IsDeleted = 1). Requires SDELETE_TICKET or admin role.
    Task<GeneralResponse> SoftDelete(int id, int currentUserId, string role);

    // Permanently removes a ticket from the database. Requires DELETE_TICKET permission.
    Task<GeneralResponse> HardDelete(int id, int currentUserId, string role);
}
