using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.models.Entities;

[Table("User")]
public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [MaxLength(50)]
    public string? Username { get; set; }

    [MaxLength(50)]
    public string? Fullname { get; set; }

    [MaxLength(60)]
    public string? Password { get; set; }

    [MaxLength(50)]
    public string? Email { get; set; }

    public int? Area { get; set; }

    public int Active { get; set; } = 1;

    public DateTime? Last_Login { get; set; }

    public int Role { get; set; }

    public int EmailConfirmed { get; set; } = 0;

    public string? EmailConfirmationToken { get; set; }

    public string? PasswordResetToken { get; set; }

    public DateTime? PasswordResetTokenExpires { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
