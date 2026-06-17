using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.models.Entities;

// Represents a registered user of the helpdesk system.
[Table("User")]
public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // Unique login name for the user (max 50 chars).
    [MaxLength(50)]
    public string? Username { get; set; }

    // Display name shown throughout the UI (max 50 chars).
    [MaxLength(50)]
    public string? Fullname { get; set; }

    // BCrypt-hashed password (max 60 chars for the hash).
    [MaxLength(60)]
    public string? Password { get; set; }

    // Email address used for notifications and account recovery (max 50 chars).
    [MaxLength(50)]
    public string? Email { get; set; }

    // FK to the Area the user belongs to (nullable).
    public int? Area { get; set; }

    // Account status: 1 = active, 0 = inactive.
    public int Active { get; set; } = 1;

    // Timestamp of the most recent successful login.
    public DateTime? Last_Login { get; set; }

    // FK to the user's assigned Role (from the Roles table).
    public int Role { get; set; }

    // Email confirmation flag: 1 = confirmed, 0 = pending.
    public int EmailConfirmed { get; set; } = 1; //change to 0 after testing 

    // Unique token sent via email for confirming the account.
    public string? EmailConfirmationToken { get; set; }

    // Token used for password reset flows.
    public string? PasswordResetToken { get; set; }

    // Expiry timestamp for the password reset token.
    public DateTime? PasswordResetTokenExpires { get; set; }

    // Timestamp when the user account was created.
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation: role assignments for this user.
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
