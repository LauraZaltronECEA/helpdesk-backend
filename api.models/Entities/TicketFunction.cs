using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.models.Entities;

[Table("Ticket_Functions")]
public class TicketFunction
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string? Function { get; set; }

    public string? Fun_Description { get; set; }
}
