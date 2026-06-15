using System.ComponentModel.DataAnnotations;

namespace api.models.DTOs;

public class RegisterDTO
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(50)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Fullname { get; set; } = string.Empty;

    public int? AreaId { get; set; }
}
