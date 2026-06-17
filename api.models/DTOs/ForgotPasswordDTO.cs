using System.ComponentModel.DataAnnotations;

namespace api.models.DTOs;

// Data required to request a password reset email.
public class ForgotPasswordDTO
{
    // The email address associated with the user's account.
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
