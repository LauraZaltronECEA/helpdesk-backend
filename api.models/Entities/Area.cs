using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.models.Entities;

// Represents a department or area within the organization.
[Table("Areas")]
public class Area
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // Name of the area/department.
    public string? Area_Name { get; set; }

    // Optional description of the area's purpose.
    public string? Description { get; set; }
}
