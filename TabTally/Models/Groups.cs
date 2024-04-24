using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Group
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Required]
    public int Id { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(50)]
    public string Name { get; set; }

    [ForeignKey("CreatedBy")]
    public virtual User? User { get; set; }

    [Required]
    public string CreatedBy { get; set; }

    [MinLength(1)]
    [MaxLength(255)]
    public string? Description { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; }

    public ICollection<GroupMembers> GroupMembers { get; set; }
}