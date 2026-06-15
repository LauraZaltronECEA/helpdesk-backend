using System.ComponentModel.DataAnnotations.Schema;

namespace api.models.Entities;

[Table("Access_To_Func_Per_Role")]
public class AccessToFuncPerRole
{
    public int Id_Role { get; set; }

    public int Id_Function { get; set; }

    [ForeignKey(nameof(Id_Role))]
    public Role Role { get; set; } = null!;

    [ForeignKey(nameof(Id_Function))]
    public TicketFunction Function { get; set; } = null!;
}
