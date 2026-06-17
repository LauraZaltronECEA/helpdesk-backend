using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.models.Entities;

// Defines a permission/function (e.g. "CREATE_TICKET", "READ_TICKET")
// that can be granted to roles via AccessToFuncPerRole.
[Table("Ticket_Functions")]
public class TicketFunction
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // Unique function/permission name used in code checks (e.g. "CREATE_TICKET").
    public string? Function { get; set; }

    // Human-readable description of what this function allows.
    public string? Fun_Description { get; set; }
}
