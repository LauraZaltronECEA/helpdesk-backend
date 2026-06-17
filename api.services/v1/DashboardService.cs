using api.models.Responses;
using api.services.Data;
using api.services.Repositories;
using Microsoft.EntityFrameworkCore;

namespace api.services.v1;

// Builds dashboard stats by querying the ticket and user tables via EF Core.
// Permission logic mirrors TicketService:
//   - users with READ_TICKET permission or admin role see all tickets
//   - other users see only their own created tickets
public class DashboardService : IDashboardRepository
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardResponse> GetDashboard(int currentUserId, string role)
    {
        var canReadAll = await HasFunction(currentUserId, "READ_TICKET");
        var isAdmin = string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase);

        // Base query: only non-deleted tickets
        var query = _db.Tickets.AsNoTracking().Where(t => t.IsDeleted == 0);

        // Non-admins without READ_TICKET permission see only their own tickets
        if (!isAdmin && !canReadAll)
        {
            query = query.Where(t => t.CreatedById == currentUserId);
        }

        // All aggregated counts in a single DB round-trip
        var counts = await query
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total      = g.Count(),
                Open       = g.Count(t => t.Status == "open"),
                InProgress = g.Count(t => t.Status == "in_progress"),
                Resolved   = g.Count(t => t.Status == "resolved"),
                Closed     = g.Count(t => t.Status == "closed"),
                Low        = g.Count(t => t.Priority == "low"),
                Medium     = g.Count(t => t.Priority == "medium"),
                High       = g.Count(t => t.Priority == "high"),
            })
            .FirstOrDefaultAsync();

        // User-specific counters (always scoped to current user)
        var myCreatedCount = await _db.Tickets
            .AsNoTracking()
            .CountAsync(t => t.IsDeleted == 0 && t.CreatedById == currentUserId);

        var myAssignedCount = await _db.Tickets
            .AsNoTracking()
            .CountAsync(t => t.IsDeleted == 0 && t.AssignedToId == currentUserId);

        // Recent 5 tickets with creator info, same scope as global query
        var recentTicketIds = await query
            .OrderByDescending(t => t.CreatedAt)
            .Take(5)
            .Select(t => t.Id)
            .ToListAsync();

        var recentTickets = await (
            from t in _db.Tickets.AsNoTracking()
            join u in _db.Users.AsNoTracking() on t.CreatedById equals u.Id
            where recentTicketIds.Contains(t.Id)
            orderby t.CreatedAt descending
            select new TicketInboxResponse
            {
                Id = t.Id,
                Title = t.Title,
                Status = t.Status,
                Priority = t.Priority,
                CreatedById = t.CreatedById,
                CreatedByUsername = u.Username ?? string.Empty,
                CreatedByFullname = u.Fullname ?? string.Empty,
                AssignedToId = t.AssignedToId,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
            })
            .ToListAsync();

        return new DashboardResponse
        {
            TotalTickets        = counts?.Total ?? 0,
            OpenTickets         = counts?.Open ?? 0,
            InProgressTickets   = counts?.InProgress ?? 0,
            ResolvedTickets     = counts?.Resolved ?? 0,
            ClosedTickets       = counts?.Closed ?? 0,
            LowPriority         = counts?.Low ?? 0,
            MediumPriority      = counts?.Medium ?? 0,
            HighPriority        = counts?.High ?? 0,
            TicketsCreatedByMe  = myCreatedCount,
            TicketsAssignedToMe = myAssignedCount,
            RecentTickets       = recentTickets,
        };
    }

    // Checks whether a user has a specific function permission via the role-permission system.
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
