using System.ComponentModel.DataAnnotations;

namespace api.models.DTOs;

// Data required to register a new user account.
public class RegisterDTO
{
    // Desired unique username (3-50 chars).
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    // Plain-text password (6-100 chars, will be BCrypt-hashed).
    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    // User's email address (used for confirmation and notifications).
    [Required]
    [EmailAddress]
    [StringLength(50)]
    public string Email { get; set; } = string.Empty;

    // Display name shown in the UI.
    [Required]
    [StringLength(50)]
    public string Fullname { get; set; } = string.Empty;

    // FK to the Area/Department the user belongs to (optional).
    public int? AreaId { get; set; }
}
