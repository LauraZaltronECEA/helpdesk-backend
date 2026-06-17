namespace api.models.Responses;

// Response returned after a successful login, extending GeneralResponse with user info and JWT.
public class LoginResponse : GeneralResponse
{
    // JWT bearer token for authenticating subsequent requests.
    public string Token { get; set; } = string.Empty;

    // ID of the authenticated user.
    public int UserId { get; set; }

    // Username of the authenticated user.
    public string Username { get; set; } = string.Empty;

    // Display name of the authenticated user.
    public string Fullname { get; set; } = string.Empty;

    // Email address of the authenticated user.
    public string Email { get; set; } = string.Empty;

    // Role name of the authenticated user (e.g. "admin", "agent", "viewer").
    public string Role { get; set; } = string.Empty;

    // Account status: 1 = active, 0 = inactive.
    public int Active { get; set; }
}
