using System.ComponentModel.DataAnnotations.Schema;

namespace api.models.Entities;

[Table("User_Roles")]
public class UserRole
{
    public int Id_User { get; set; }

    public int Id_Roles { get; set; }

    [ForeignKey(nameof(Id_User))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(Id_Roles))]
    public Role Role { get; set; } = null!;
}
