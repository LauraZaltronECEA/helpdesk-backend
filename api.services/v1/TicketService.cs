using System.Text.RegularExpressions;
using api.models.DTOs;
using api.models.Entities;
using api.models.Responses;
using api.services.Data;
using api.services.Repositories;
using Microsoft.EntityFrameworkCore;

namespace api.services.v1;

// Implements ticket CRUD with role-based and permission-based access control.
// Uses the custom HasFunction() helper to check granular permissions via the
// Role -> AccessToFuncPerRole -> TicketFunction relationship.
public class TicketService : ITicketRepository
{
    private readonly AppDbContext _db;

    public TicketService(AppDbContext db)
    {
        _db = db;
    }

    // Returns all non-deleted tickets. Users with READ_TICKET permission or admin role
    // see all tickets; others see only tickets they created.
    public async Task<IEnumerable<Ticket>> GetAll(int currentUserId, string role)
    {
        var canReadAll = await HasFunction(currentUserId, "READ_TICKET");

        var query = _db.Tickets.AsNoTracking().Where(t => t.IsDeleted == 0);

        if (!canReadAll && !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(t => t.CreatedById == currentUserId);
        }

        return await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
    }

    // Returns tickets for the user's inbox view, with creator/assignee names.
    // Admin: all tickets. Agent (READ_TICKET): assigned tickets. Viewer: own tickets.
    public async Task<IEnumerable<TicketInboxResponse>> GetInbox(int currentUserId, string role)
    {
        var canReadAll = await HasFunction(currentUserId, "READ_TICKET");

        var query = _db.Tickets.AsNoTracking().Where(t => t.IsDeleted == 0);

        if (string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
        {
            // admin sees all tickets
        }
        else if (canReadAll)
        {
            // agents see tickets assigned to them
            query = query.Where(t => t.AssignedToId == currentUserId);
        }
        else
        {
            // viewers see only their own tickets
            query = query.Where(t => t.CreatedById == currentUserId);
        }

        return await (
            from ticket in query
            join creator in _db.Users.AsNoTracking() on ticket.CreatedById equals creator.Id
            join assignedUser in _db.Users.AsNoTracking() on ticket.AssignedToId equals assignedUser.Id into assignedUsers
            from assigned in assignedUsers.DefaultIfEmpty()
            orderby ticket.CreatedAt descending
            select new TicketInboxResponse
            {
                Id = ticket.Id,
                Title = ticket.Title,
                Status = ticket.Status,
                Priority = ticket.Priority,
                CreatedById = ticket.CreatedById,
                CreatedByUsername = creator.Username ?? string.Empty,
                CreatedByFullname = creator.Fullname ?? string.Empty,
                AssignedToId = ticket.AssignedToId,
                AssignedToUsername = assigned == null ? null : assigned.Username,
                AssignedToFullname = assigned == null ? null : assigned.Fullname,
                CreatedAt = ticket.CreatedAt,
                UpdatedAt = ticket.UpdatedAt
            }).ToListAsync();
    }

    // Gets a single ticket by ID. Accessible to admins, users with READ_TICKET,
    // or the ticket's creator. Returns null otherwise.
    public async Task<Ticket?> GetById(int id, int currentUserId, string role)
    {
        var canReadAll = await HasFunction(currentUserId, "READ_TICKET");

        var ticket = await _db.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id && t.IsDeleted == 0);

        if (ticket == null) return null;
        if (canReadAll || string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase)) return ticket;
        if (ticket.CreatedById == currentUserId) return ticket;

        return null;
    }

    // Gets a single ticket with full detail. Accessible to admins, users with READ_TICKET,
    // the creator, or the assigned user. Returns null otherwise.
    public async Task<TicketDetailResponse?> GetDetail(int id, int currentUserId, string role)
    {
        var canReadAll = await HasFunction(currentUserId, "READ_TICKET");

        var ticketData = await (
            from ticket in _db.Tickets.AsNoTracking()
            join creator in _db.Users.AsNoTracking() on ticket.CreatedById equals creator.Id
            join assignedUser in _db.Users.AsNoTracking() on ticket.AssignedToId equals assignedUser.Id into assignedUsers
            from assigned in assignedUsers.DefaultIfEmpty()
            where ticket.Id == id && ticket.IsDeleted == 0
            select new
            {
                Ticket = ticket,
                Creator = creator,
                Assigned = assigned
            }).FirstOrDefaultAsync();

        if (ticketData == null) return null;

        var canView = canReadAll
            || string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase)
            || ticketData.Ticket.CreatedById == currentUserId
            || ticketData.Ticket.AssignedToId == currentUserId;

        if (!canView) return null;

        return new TicketDetailResponse
        {
            Id = ticketData.Ticket.Id,
            Title = ticketData.Ticket.Title,
            Description = ticketData.Ticket.Description,
            Status = ticketData.Ticket.Status,
            Priority = ticketData.Ticket.Priority,
            CreatedById = ticketData.Ticket.CreatedById,
            CreatedByUsername = ticketData.Creator.Username ?? string.Empty,
            CreatedByFullname = ticketData.Creator.Fullname ?? string.Empty,
            CreatedByEmail = ticketData.Creator.Email ?? string.Empty,
            AssignedToId = ticketData.Ticket.AssignedToId,
            AssignedToUsername = ticketData.Assigned?.Username,
            AssignedToFullname = ticketData.Assigned?.Fullname,
            AssignedToEmail = ticketData.Assigned?.Email,
            CreatedAt = ticketData.Ticket.CreatedAt,
            UpdatedAt = ticketData.Ticket.UpdatedAt
        };
    }

    // Creates a new ticket. Requires the CREATE_TICKET permission.
    // Validates required fields and normalizes priority to lowercase.
    public async Task<TicketCreateResponse> Create(TicketCreateDTO dto, int createdById)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return new TicketCreateResponse { Estado = false, Codigo = 400, Mensaje = "Title is required" };
        if (string.IsNullOrWhiteSpace(dto.Description))
            return new TicketCreateResponse { Estado = false, Codigo = 400, Mensaje = "Description is required" };

        // Permission check
        var canCreate = await HasFunction(createdById, "CREATE_TICKET");
        if (!canCreate)
            return new TicketCreateResponse { Estado = false, Codigo = 403, Mensaje = "No tienes permiso para crear tickets" };

        // Validate priority format
        var normalizedPriority = dto.Priority?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(normalizedPriority) && !Regex.IsMatch(normalizedPriority, "^(low|medium|high)$"))
        {
            return new TicketCreateResponse { Estado = false, Codigo = 400, Mensaje = "Priority must be one of: low, medium, high." };
        }

        var ticket = new Ticket
        {
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            Priority = normalizedPriority ?? "medium",
            CreatedById = createdById,
            CreatedAt = DateTime.UtcNow
        };

        _db.Tickets.Add(ticket);

        try
        {
            await _db.SaveChangesAsync();
            return new TicketCreateResponse { Estado = true, Codigo = 1, Mensaje = "Ticket created", TicketId = ticket.Id };
        }
        catch (DbUpdateException ex)
        {
            return new TicketCreateResponse
            {
                Estado = false,
                Codigo = 500,
                Mensaje = $"Database error: {ex.InnerException?.Message ?? ex.Message}"
            };
        }
    }

    // Updates an existing ticket. Requires UPDATE_TICKET permission or admin role.
    // The user must also be the creator or the assigned user (unless admin).
    // Only provided fields are applied; null fields keep their current values.
    public async Task<GeneralResponse> Update(int id, TicketUpdateDTO dto, int currentUserId, string role)
    {
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == id && t.IsDeleted == 0);
        if (ticket == null)
            return new GeneralResponse { Estado = false, Codigo = 404, Mensaje = "Not found" };

        // Permission check
        var canUpdate = await HasFunction(currentUserId, "UPDATE_TICKET");
        if (!canUpdate && !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
            return new GeneralResponse { Estado = false, Codigo = 403, Mensaje = "Forbidden" };

        // Non-admins must be the creator or assigned user
        if (!string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase)
            && ticket.CreatedById != currentUserId
            && ticket.AssignedToId != currentUserId)
        {
            return new GeneralResponse { Estado = false, Codigo = 403, Mensaje = "Forbidden" };
        }

        if (dto.Title != null) ticket.Title = dto.Title.Trim();
        if (dto.Description != null) ticket.Description = dto.Description.Trim();
        if (dto.Status != null) ticket.Status = dto.Status.Trim().ToLowerInvariant();
        if (dto.Priority != null) ticket.Priority = dto.Priority.Trim().ToLowerInvariant();
        if (dto.AssignedToId.HasValue) ticket.AssignedToId = dto.AssignedToId;
        ticket.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync();
            return new GeneralResponse { Estado = true, Codigo = 1, Mensaje = "Ticket updated" };
        }
        catch (DbUpdateException ex)
        {
            return new GeneralResponse
            {
                Estado = false,
                Codigo = 500,
                Mensaje = $"Database error: {ex.InnerException?.Message ?? ex.Message}"
            };
        }
    }

    // Soft-deletes a ticket by setting IsDeleted = 1.
    // Requires SDELETE_TICKET permission or admin role. Non-admins must be the creator.
    public async Task<GeneralResponse> SoftDelete(int id, int currentUserId, string role)
    {
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == id && t.IsDeleted == 0);
        if (ticket == null)
            return new GeneralResponse { Estado = false, Codigo = 404, Mensaje = "Not found" };

        // Permission check
        var canSoftDelete = await HasFunction(currentUserId, "SDELETE_TICKET");
        if (!canSoftDelete && !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
            return new GeneralResponse { Estado = false, Codigo = 403, Mensaje = "Forbidden" };

        // Non-admins must be the creator
        if (!string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase)
            && ticket.CreatedById != currentUserId)
        {
            return new GeneralResponse { Estado = false, Codigo = 403, Mensaje = "Forbidden" };
        }

        ticket.IsDeleted = 1;
        ticket.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync();
            return new GeneralResponse { Estado = true, Codigo = 1, Mensaje = "Ticket deleted" };
        }
        catch (DbUpdateException ex)
        {
            return new GeneralResponse
            {
                Estado = false,
                Codigo = 500,
                Mensaje = $"Database error: {ex.InnerException?.Message ?? ex.Message}"
            };
        }
    }

    // Permanently removes a ticket from the database.
    // Requires the DELETE_TICKET permission (admin-only by default).
    public async Task<GeneralResponse> HardDelete(int id, int currentUserId, string role)
    {
        var canHardDelete = await HasFunction(currentUserId, "DELETE_TICKET");
        if (!canHardDelete)
            return new GeneralResponse { Estado = false, Codigo = 403, Mensaje = "Forbidden" };

        var ticket = await _db.Tickets.FindAsync(id);
        if (ticket == null)
            return new GeneralResponse { Estado = false, Codigo = 404, Mensaje = "Not found" };

        _db.Tickets.Remove(ticket);

        try
        {
            await _db.SaveChangesAsync();
            return new GeneralResponse { Estado = true, Codigo = 1, Mensaje = "Ticket permanently deleted" };
        }
        catch (DbUpdateException ex)
        {
            return new GeneralResponse
            {
                Estado = false,
                Codigo = 500,
                Mensaje = $"Database error: {ex.InnerException?.Message ?? ex.Message}"
            };
        }
    }

    // Checks whether a user has a specific function permission by traversing the
    // UserRole -> AccessToFuncPerRole -> TicketFunction relationship chain.
    private async Task<bool> HasFunction(int userId, string functionName)
    {
        return await (
            from ur in _db.UserRoles
            join a in _db.AccessToFuncPerRoles on ur.Id_Roles equals a.Id_Role
            join f in _db.TicketFunctions on a.Id_Function equals f.Id
            where ur.Id_User == userId && f.Function == functionName
            select 1
        ).AnyAsync();
    }
}
