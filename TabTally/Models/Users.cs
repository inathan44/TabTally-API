using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
public class User
{
    [Key]
    [Required]
    public string Id { get; set; }

    [MinLength(3)]
    [MaxLength(50)]
    public string? Username { get; set; }

    public string? Email { get; set; }

    [MinLength(1)]
    [MaxLength(50)]
    public string? FirstName { get; set; }


    [MinLength(1)]
    [MaxLength(50)]
    public string? LastName { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; }

    public ICollection<GroupMember> GroupMembers { get; set; }
}