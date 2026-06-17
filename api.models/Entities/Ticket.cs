using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.models.Entities;

// Represents a support ticket created by a user in the helpdesk system.
[Table("Tickets")]
public class Ticket
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // Short summary of the issue (required, max 200 chars).
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    // Detailed description of the issue (required, max 4000 chars).
    [Required]
    [MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    // Current lifecycle status: "open", "in_progress", "resolved", or "closed".
    [MaxLength(50)]
    public string Status { get; set; } = "open";

    // Severity level: "low", "medium", or "high".
    [MaxLength(50)]
    public string Priority { get; set; } = "medium";

    // FK to the User who created the ticket.
    public int CreatedById { get; set; }

    // Timestamp when the ticket was created.
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // FK to the User assigned to resolve the ticket (nullable).
    public int? AssignedToId { get; set; }

    // Timestamp of the last update (nullable).
    public DateTime? UpdatedAt { get; set; }

    // Soft-delete flag: 0 = active, 1 = deleted.
    public int IsDeleted { get; set; } = 0;

    // Navigation property to the User who created the ticket.
    [ForeignKey(nameof(CreatedById))]
    public User CreatedBy { get; set; } = null!;

    // Navigation property to the User assigned to the ticket (nullable).
    [ForeignKey(nameof(AssignedToId))]
    public User? AssignedTo { get; set; }
}
