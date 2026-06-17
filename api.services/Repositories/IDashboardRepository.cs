using api.models.Responses;

namespace api.services.Repositories;

// Provides aggregated dashboard data scoped to the current user's role and permissions.
public interface IDashboardRepository
{
    // Builds and returns a DashboardResponse for the given user.
    // currentUserId: ID of the authenticated user.
    // role: Role claim from JWT ("admin" or "viewer"/others).
    Task<DashboardResponse> GetDashboard(int currentUserId, string role);
}
