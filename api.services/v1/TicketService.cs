using System.Text.RegularExpressions;
using api.models.DTOs;
using api.models.Entities;
using api.models.Responses;
using api.services.Data;
using api.services.Repositories;
using Microsoft.EntityFrameworkCore;

namespace api.services.v1;

public class TicketService : ITicketRepository
{
    private readonly AppDbContext _db;

    public TicketService(AppDbContext db)
    {
        _db = db;
    }

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

    public async Task<IEnumerable<TicketInboxResponse>> GetInbox(int currentUserId, string role)
    {
        var canReadAll = await HasFunction(currentUserId, "READ_TICKET");

        var query = _db.Tickets.AsNoTracking().Where(t => t.IsDeleted == 0);

        if (string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
        {
            // admin sees all
        }
        else if (canReadAll)
        {
            // agent sees assigned tickets
            query = query.Where(t => t.AssignedToId == currentUserId);
        }
        else
        {
            // viewer sees own tickets
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

    public async Task<TicketCreateResponse> Create(TicketCreateDTO dto, int createdById)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return new TicketCreateResponse { Estado = false, Codigo = 400, Mensaje = "Title is required" };
        if (string.IsNullOrWhiteSpace(dto.Description))
            return new TicketCreateResponse { Estado = false, Codigo = 400, Mensaje = "Description is required" };

        var canCreate = await HasFunction(createdById, "CREATE_TICKET");
        if (!canCreate)
            return new TicketCreateResponse { Estado = false, Codigo = 403, Mensaje = "No tienes permiso para crear tickets" };

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

    public async Task<GeneralResponse> Update(int id, TicketUpdateDTO dto, int currentUserId, string role)
    {
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == id && t.IsDeleted == 0);
        if (ticket == null)
            return new GeneralResponse { Estado = false, Codigo = 404, Mensaje = "Not found" };

        var canUpdate = await HasFunction(currentUserId, "UPDATE_TICKET");
        if (!canUpdate && !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
            return new GeneralResponse { Estado = false, Codigo = 403, Mensaje = "Forbidden" };

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

    public async Task<GeneralResponse> SoftDelete(int id, int currentUserId, string role)
    {
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == id && t.IsDeleted == 0);
        if (ticket == null)
            return new GeneralResponse { Estado = false, Codigo = 404, Mensaje = "Not found" };

        var canSoftDelete = await HasFunction(currentUserId, "SDELETE_TICKET");
        if (!canSoftDelete && !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
            return new GeneralResponse { Estado = false, Codigo = 403, Mensaje = "Forbidden" };

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
