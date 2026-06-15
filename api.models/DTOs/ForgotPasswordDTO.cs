using System.ComponentModel.DataAnnotations;

namespace api.models.DTOs;

public class ForgotPasswordDTO
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
