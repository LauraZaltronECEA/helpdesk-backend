using System.ComponentModel.DataAnnotations.Schema;

namespace api.models.Entities;

// Join table linking users to their roles (many-to-many).
[Table("User_Roles")]
public class UserRole
{
    // FK to the User.
    public int Id_User { get; set; }

    // FK to the Role.
    public int Id_Roles { get; set; }

    // Navigation property to the related User.
    [ForeignKey(nameof(Id_User))]
    public User User { get; set; } = null!;

    // Navigation property to the related Role.
    [ForeignKey(nameof(Id_Roles))]
    public Role Role { get; set; } = null!;
}
