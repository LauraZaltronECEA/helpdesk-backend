using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.models.Entities;

[Table("Tickets")]
public class Ticket
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Status { get; set; } = "open";

    [MaxLength(50)]
    public string Priority { get; set; } = "medium";

    public int CreatedById { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? AssignedToId { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int IsDeleted { get; set; } = 0;

    [ForeignKey(nameof(CreatedById))]
    public User CreatedBy { get; set; } = null!;

    [ForeignKey(nameof(AssignedToId))]
    public User? AssignedTo { get; set; }
}
