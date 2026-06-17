using System.Security.Claims;
using api.models.Responses;
using api.services.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.helpdesk.Controllers.v1;

// Returns aggregated dashboard data for the authenticated user.
// Users with admin role or READ_TICKET permission see global stats;
// regular users see only their own tickets.
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class DashboardController : ControllerBase
{
    private readonly IDashboardRepository _dashboardRepo;

    public DashboardController(IDashboardRepository dashboardRepo)
    {
        _dashboardRepo = dashboardRepo;
    }

    // Reads the Role claim from the JWT; defaults to "viewer".
    private string CurrentRole => User.FindFirstValue(ClaimTypes.Role) ?? "viewer";

    // Attempts to extract the user's integer ID from multiple JWT claim types.
    // Checks NameIdentifier, nameid, and sub.
    private bool TryGetCurrentUserId(out int currentUserId)
    {
        currentUserId = 0;

        var claimValues = User.FindAll(ClaimTypes.NameIdentifier)
            .Select(c => c.Value)
            .Concat(User.FindAll("nameid").Select(c => c.Value))
            .Concat(User.FindAll("sub").Select(c => c.Value));

        foreach (var value in claimValues)
        {
            if (int.TryParse(value, out currentUserId))
                return true;
        }

        return false;
    }

    // GET /api/v1/dashboard — returns the dashboard response.
    [HttpGet]
    public async Task<ActionResult<DashboardResponse>> Get()
    {
        if (!TryGetCurrentUserId(out int currentUserId))
            return Unauthorized();

        var dashboard = await _dashboardRepo.GetDashboard(currentUserId, CurrentRole);
        return Ok(dashboard);
    }
}
