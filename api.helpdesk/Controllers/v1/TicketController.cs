using System.Security.Claims;
using api.models.DTOs;
using api.models.Entities;
using api.models.Responses;
using api.services.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.helpdesk.Controllers.v1;

// CRUD endpoints for tickets. All endpoints require a valid JWT.
// Access is scoped by role ("admin") and granular permissions (e.g. READ_TICKET, CREATE_TICKET).
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class TicketController : ControllerBase
{
    private readonly ITicketRepository _ticketRepo;

    public TicketController(ITicketRepository ticketRepo)
    {
        _ticketRepo = ticketRepo;
    }

    // Reads the Role claim from the JWT. Defaults to "viewer" if no role claim is present.
    private string CurrentRole => User.FindFirstValue(ClaimTypes.Role) ?? "viewer";

    // Attempts to extract the user's integer ID from multiple JWT claim types
    // (NameIdentifier, nameid, sub). Returns true if a valid ID is found.
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

    // GET /api/v1/ticket — Returns all accessible tickets (list view, no user details).
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Ticket>>> GetAll()
    {
        if (!TryGetCurrentUserId(out int currentUserId))
            return Unauthorized();

        var tickets = await _ticketRepo.GetAll(currentUserId, CurrentRole);
        return Ok(tickets);
    }

    // GET /api/v1/ticket/inbox — Returns tickets in the user's inbox, with user names.
    [HttpGet("inbox")]
    public async Task<ActionResult<IEnumerable<TicketInboxResponse>>> GetInbox()
    {
        if (!TryGetCurrentUserId(out int currentUserId))
            return Unauthorized();

        var tickets = await _ticketRepo.GetInbox(currentUserId, CurrentRole);
        return Ok(tickets);
    }

    // GET /api/v1/ticket/{id} — Returns a single ticket by ID (no user details).
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Ticket>> GetById(int id)
    {
        if (!TryGetCurrentUserId(out int currentUserId))
            return Unauthorized();

        var ticket = await _ticketRepo.GetById(id, currentUserId, CurrentRole);
        if (ticket == null) return Forbid();
        return Ok(ticket);
    }

    // GET /api/v1/ticket/{id}/detail — Returns a single ticket with full user/contact info.
    [HttpGet("{id:int}/detail")]
    public async Task<ActionResult<TicketDetailResponse>> GetDetail(int id)
    {
        if (!TryGetCurrentUserId(out int currentUserId))
            return Unauthorized();

        var ticket = await _ticketRepo.GetDetail(id, currentUserId, CurrentRole);
        if (ticket == null) return Forbid();
        return Ok(ticket);
    }

    // POST /api/v1/ticket — Creates a new ticket for the authenticated user.
    [HttpPost]
    public async Task<ActionResult<TicketCreateResponse>> Create([FromBody] TicketCreateDTO? dto)
    {
        if (dto == null)
            return BadRequest(new TicketCreateResponse { Estado = false, Codigo = 400, Mensaje = "Request body is required" });

        if (!TryGetCurrentUserId(out int currentUserId))
            return Unauthorized();

        var result = await _ticketRepo.Create(dto, currentUserId);
        return result.Estado
            ? CreatedAtAction(nameof(GetById), new { id = result.TicketId }, result)
            : result.Codigo == 403 ? Forbid() : BadRequest(result);
    }

    // PUT /api/v1/ticket/{id} — Updates an existing ticket (partial update).
    [HttpPut("{id:int}")]
    public async Task<ActionResult<GeneralResponse>> Update(int id, [FromBody] TicketUpdateDTO? dto)
    {
        if (dto == null)
            return BadRequest(new GeneralResponse { Estado = false, Codigo = 400, Mensaje = "Request body is required" });

        if (!TryGetCurrentUserId(out int currentUserId))
            return Unauthorized();

        var result = await _ticketRepo.Update(id, dto, currentUserId, CurrentRole);
        if (result.Codigo == 403) return Forbid();
        if (result.Codigo == 404) return NotFound(result);
        return Ok(result);
    }

    // DELETE /api/v1/ticket/{id} — Soft-deletes a ticket (sets IsDeleted = 1).
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<GeneralResponse>> SoftDelete(int id)
    {
        if (!TryGetCurrentUserId(out int currentUserId))
            return Unauthorized();

        var result = await _ticketRepo.SoftDelete(id, currentUserId, CurrentRole);
        if (result.Codigo == 403) return Forbid();
        if (result.Codigo == 404) return NotFound(result);
        return Ok(result);
    }

    // DELETE /api/v1/ticket/{id}/hard — Permanently removes a ticket. Admin-only via [Authorize(Roles = "admin")].
    [HttpDelete("{id:int}/hard")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "admin")]
    public async Task<ActionResult<GeneralResponse>> HardDelete(int id)
    {
        if (!TryGetCurrentUserId(out int currentUserId))
            return Unauthorized();

        var result = await _ticketRepo.HardDelete(id, currentUserId, CurrentRole);
        if (result.Codigo == 403) return Forbid();
        if (result.Codigo == 404) return NotFound(result);
        return Ok(result);
    }
}
