using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class GroupMembers
{

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Required]
    public int Id { get; set; }

    [ForeignKey("GroupId")]
    public Group Group { get; set; }
    [Required]
    public int GroupId { get; set; }


    [Required]
    public string UserId { get; set; }

    [ForeignKey("UserId")]
    public User User { get; set; }

    public bool IsAdmin { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; }
}