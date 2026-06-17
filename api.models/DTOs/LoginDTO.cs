using System.ComponentModel.DataAnnotations;

namespace api.models.DTOs;

// Credentials required for user login.
public class LoginDTO
{
    // The user's username (required).
    [Required]
    public string Username { get; set; } = string.Empty;

    // The user's plain-text password (required).
    [Required]
    public string Password { get; set; } = string.Empty;
}
