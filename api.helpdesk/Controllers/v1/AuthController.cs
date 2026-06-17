using api.models.DTOs;
using api.models.Responses;
using api.services.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace api.helpdesk.Controllers.v1;

// Public endpoints for user authentication and registration.
// No authorization required — registration, login, and password reset must be open.
[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthRepository _authRepository;

    public AuthController(IAuthRepository authRepository)
    {
        _authRepository = authRepository;
    }

    // POST /api/v1/auth/login — Authenticates a user and returns a JWT.
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginDTO dto)
    {
        if (dto == null)
            return BadRequest(new GeneralResponse { Estado = false, Codigo = 400, Mensaje = "Request body is required" });

        var result = await _authRepository.Login(dto);
        if (!result.Estado)
            return Unauthorized(result);

        return Ok(result);
    }

    // POST /api/v1/auth/register — Creates a new account and sends a confirmation email.
    [HttpPost("register")]
    public async Task<ActionResult<GeneralResponse>> Register([FromBody] RegisterDTO dto)
    {
        if (dto == null)
            return BadRequest(new GeneralResponse { Estado = false, Codigo = 400, Mensaje = "Request body is required" });

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await _authRepository.Register(dto, baseUrl);

        if (!result.Estado)
            return BadRequest(result);

        return Ok(result);
    }

    // GET /api/v1/auth/confirm-email — Confirms the user's email via the token from the email link.
    [HttpGet("confirm-email")]
    public async Task<ActionResult<GeneralResponse>> ConfirmEmail([FromQuery] int userId, [FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new GeneralResponse { Estado = false, Codigo = 400, Mensaje = "Token is required" });

        var result = await _authRepository.ConfirmEmail(userId, token);

        if (!result.Estado)
            return BadRequest(result);

        return Ok(result);
    }

    // POST /api/v1/auth/forgot-password — Sends a password reset email if the email is found.
    [HttpPost("forgot-password")]
    public async Task<ActionResult<GeneralResponse>> ForgotPassword([FromBody] ForgotPasswordDTO dto)
    {
        if (dto == null)
            return BadRequest(new GeneralResponse { Estado = false, Codigo = 400, Mensaje = "Request body is required" });

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await _authRepository.ForgotPassword(dto, baseUrl);

        return Ok(result);
    }

    // POST /api/v1/auth/reset-password — Resets the password using the token from the email.
    [HttpPost("reset-password")]
    public async Task<ActionResult<GeneralResponse>> ResetPassword([FromBody] ResetPasswordDTO dto)
    {
        if (dto == null)
            return BadRequest(new GeneralResponse { Estado = false, Codigo = 400, Mensaje = "Request body is required" });

        var result = await _authRepository.ResetPassword(dto);

        if (!result.Estado)
            return BadRequest(result);

        return Ok(result);
    }
}
