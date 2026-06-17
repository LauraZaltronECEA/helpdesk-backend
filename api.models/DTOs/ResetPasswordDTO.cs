using System.ComponentModel.DataAnnotations;

namespace api.models.DTOs;

// Data required to complete a password reset.
public class ResetPasswordDTO
{
    // The user's email address.
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    // The reset token received via email.
    [Required]
    public string Token { get; set; } = string.Empty;

    // The new password (6-100 chars).
    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string NewPassword { get; set; } = string.Empty;
}
