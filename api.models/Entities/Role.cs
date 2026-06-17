using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.models.Entities;

// Defines a named role (e.g. "admin", "agent", "viewer") used for authorization.
[Table("Roles")]
public class Role
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // Human-readable role name (e.g. "admin", "agent", "viewer").
    public string? Role_Name { get; set; }

    // Navigation: users assigned to this role.
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    // Navigation: function permissions granted to this role.
    public ICollection<AccessToFuncPerRole> AccessToFuncPerRoles { get; set; } = new List<AccessToFuncPerRole>();
}
