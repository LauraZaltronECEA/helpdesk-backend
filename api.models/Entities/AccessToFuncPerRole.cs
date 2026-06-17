using System.ComponentModel.DataAnnotations.Schema;

namespace api.models.Entities;

// Join table linking roles to their permitted functions (many-to-many).
[Table("Access_To_Func_Per_Role")]
public class AccessToFuncPerRole
{
    // FK to the Role.
    public int Id_Role { get; set; }

    // FK to the TicketFunction (permission).
    public int Id_Function { get; set; }

    // Navigation property to the related Role.
    [ForeignKey(nameof(Id_Role))]
    public Role Role { get; set; } = null!;

    // Navigation property to the related TicketFunction.
    [ForeignKey(nameof(Id_Function))]
    public TicketFunction Function { get; set; } = null!;
}
